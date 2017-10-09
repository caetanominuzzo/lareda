using System.IO.Ports;
using System.Web;
using library.core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using library;
using System.Collections.Specialized;
using System.Windows.Forms;
using windows_desktop.Properties;
using System.Drawing.Imaging;
using System.Drawing;
using Devcorner.NIdenticon;

namespace windows_desktop
{

    public class WebServer : IDisposable
    {
        public class HttpListenerCallbackState
        {
            private readonly HttpListener _listener;

            private readonly AutoResetEvent _listenForNextRequest;

            public HttpListenerCallbackState(HttpListener listener)
            {
                if (listener == null) throw new ArgumentNullException("listener");
                _listener = listener;
                _listenForNextRequest = new AutoResetEvent(false);
            }

            public HttpListener Listener { get { return _listener; } }
            public AutoResetEvent ListenForNextRequest { get { return _listenForNextRequest; } }
        }

        internal static ManualResetEvent stopEvent = new ManualResetEvent(false);

        public delegate void DraggingHandler(bool dragging, string dragId, string userAddress);

        static public event DraggingHandler OnDragging;

        internal static Cache<FileDownloadObject> downloads = new Cache<FileDownloadObject>(10000);

        internal static Cache<string> keepAlive = new Cache<string>(1000000);

        static Cache<SearchResult> searchs = new Cache<SearchResult>(60000);

        public delegate void FileWriteHandler(string filename, int[] cursors);

        public static event FileWriteHandler OnFileWrite;

        #region Thread Refresh

        static Thread thread;

        static bool stop = false;

        internal static IDisposable Start()
        {
            //todo: pra fechar a conexão
            downloads.OnCacheExpired += Downloads_OnCacheExpired;

            var ws = new WebServer();

            if (thread == null)
            {
                thread = new Thread(ws.Configure);

                thread.Start();
            }


            return ws;
        }

        private static void Downloads_OnCacheExpired(CacheItem<FileDownloadObject> item)
        {

        }

        private static void Searchs_OnCacheExpired(CacheItem<SearchResult> item)
        {

        }

        public void Dispose()
        {
            stop = true;

            stopEvent.Set();
        }

        public void Configure()
        {
            //Client.OnSearchReturn += Client_onSearchReturn;

            Client.OnFileDownload += Client_OnFileDownload;

            try
            {

                HttpListener listener = new HttpListener();

                listener.Prefixes.Add("http://+:" + Program.WebPort + "/");

                listener.Start();

                HttpListenerCallbackState state = new HttpListenerCallbackState(listener);

                ThreadPool.QueueUserWorkItem(ThreadReceiveNew, state);

                stopEvent.WaitOne();

            }
            catch (HttpListenerException e)
            {
                if (Program.RunAsAdministrator(AppDomain.CurrentDomain.FriendlyName, "NETSH"))
                {
                    Configure();

                    return;
                }
            }

            //ThreadReceive();
        }

        private void ThreadReceiveNew(object state)
        {
            HttpListenerCallbackState callbackState = (HttpListenerCallbackState)state;

            while (callbackState.Listener.IsListening)
            {
                callbackState.Listener.BeginGetContext(new AsyncCallback(ListenerCallback), callbackState);
                int n = WaitHandle.WaitAny(new WaitHandle[] { callbackState.ListenForNextRequest, stopEvent });

                if (n == 1)
                {
                    // stopEvent was signalled 
                    callbackState.Listener.Stop();
                    break;
                }
            }


            //ProcessReceive(new p2pContext(context));

            ////ThreadPool.QueueUserWorkItem(ProcessReceive, new p2pContext(context));

            ////context.JsonResponse(new { Hola = "Message" });
        }

        private void ListenerCallback(IAsyncResult ar)
        {
            HttpListenerCallbackState callbackState = (HttpListenerCallbackState)ar.AsyncState;
            HttpListenerContext context = null;

            try
            {
                context = callbackState.Listener.EndGetContext(ar);
            }
            catch (Exception ex)
            {
                return;
            }
            finally
            {
                callbackState.ListenForNextRequest.Set();
            }

            if (context == null) return;


            HttpListenerRequest request = context.Request;

            try
            {
                using (HttpListenerResponse response = context.Response)
                {
                    ProcessReceive(new p2pContext(context));
                }
            }
            catch (Exception e)
            {
            }
        }


        #endregion

        static void ProcessReceive(object o)
        {
            var context = (p2pContext)o;

            if (context.HttpContext.Request.RawUrl.Contains("favico"))
            {
                CloseResponse(context);

                return;
            }

            var response = context.HttpContext.Response;

            //response.AddHeader("Access-Control-Allow-Origin", "*");

            var baseAddress = GetSegment(context.HttpContext, 1);

            if (baseAddress == null || Utils.AddressFromBase64String(baseAddress) == null)
            {
                response.RedirectLocation = pParameters.webHome + "/index.html";

                response.Redirect("/" + pParameters.webHome + "/");

                //context.Response.StatusCode = (int)HttpStatusCode.Ambiguous;

                CloseResponse(context);

                return;
            }

            var command = string.Empty;

            if (baseAddress == pParameters.webHome || baseAddress == pParameters.welcomeHome)
                command = GetSegment(context.HttpContext, 2);

            switch (command)
            {
                //#if DEBUG
                default:

                    if (command != null && command.StartsWith("debug:"))
                    {
                        var cmd = command.Substring(6);

                        var par = cmd.Split(':');

                        var method = typeof(Client).GetMethod(par[0]);

                        var res = method.Invoke(null, par.Length == 1 ? null : new string[] { par[1] });

                        CloseResponse(context, res == null ? string.Empty : res.ToString());
                    }
                    else
                    {
                        ProcessGet(context);
                    }

                    break;


                //#else
                //              default:

                //                    ProcessGet(context);

                //                    break;

                //#endif
                case WebCommands.Search:

                    ProcessSearch(context);

                    break;

                case WebCommands.CreateUserAvatar:

                    ProcessCreateUserAvatar(context);

                    break;

                case WebCommands.KeepAlive:

                    ProcessKeepAlive(context);

                    break;

                case WebCommands.FriendKey:

                    var key = Client.GetWelcomeKey();

                    CloseResponse(context, key);

                    break;

                case WebCommands.AddPeer:

                    ProcessAddPeer(context);

                    break;

                case WebCommands.Context:

                    ProcessContext(context);

                    break;

                case WebCommands.Post:

                    ProcessPost(context);

                    break;

                case WebCommands.FileUpload:

                    //var files = JsonConvert.DeserializeObject(HttpUtility.UrlDecode(requestParams));

                    break;

                case WebCommands.DragOver:
                case WebCommands.DragLeave:

                    ProcessDrag(context, command);

                    break;
            }
        }



        static void ProcessContext(p2pContext context)
        {
            var data = ProcessForm(context);

            var contextId = data["context"];

            if (string.IsNullOrWhiteSpace(contextId))
            {
                CloseResponse(context);

                return;
            }

            string result = null;

            var bContextId = Utils.AddressFromBase64String(contextId);

            byte[] buffer;

            string json = string.Empty;

            SearchResult search = null;

            lock (searchs)
            {
                var searchItem = searchs.FirstOrDefault(x => Addresses.Equals(x.CachedValue.ContextId, bContextId, true));

                if (searchItem != null)
                {
                    searchItem.Reset();

                    search = searchItem.CachedValue;
                }
                else
                {

                }

                if (search != null)
                    result = search.GetResultsResults(context);//search.Mode);
            }

            if (string.IsNullOrEmpty(result))
                result = "[]";

            CloseResponse(context, result);

            if (result != null)
                result.ToList().ForEach(x =>
                {

                    //if (searchs.Searched(x.HitValue.Address, byteContextId) == 0)
                    //    Search(contextId, x.HitValue.Base64Address);

                    //if (x.HitValue.TargetBase64Address != null)
                    //    if (searchs.Searched(x.HitValue.targetAddress, byteContextId) == 0)
                    //        Search(contextId, x.HitValue.TargetBase64Address);

                    //if (Utils.AddressFromBase64String(x.HitValue.Content) != null)
                    //    if (searchs.Searched(Utils.AddressFromBase64String(x.HitValue.Content), byteContextId) == 0)
                    //        Search(contextId, x.HitValue.Content);

                });
        }

        static Dictionary<string, string> ProcessForm(p2pContext context)
        {
            var result = new Dictionary<string, string>();

            string content;

            using (var reader = new StreamReader(context.HttpContext.Request.InputStream,
                                     context.HttpContext.Request.ContentEncoding))
            {
                content = reader.ReadToEnd();
            }

            if (content.Length == 0)
                return result;

            string[] rawParams = content.Split('&');

            foreach (string param in rawParams)
            {
                string[] kvPair = param.Split('=');
                string key = kvPair[0];
                string value = HttpUtility.UrlDecode(kvPair[1]);
                result.Add(key, value);
            }

            return result;
        }

        static void ProcessSearch(p2pContext context)
        {
            var data = ProcessForm(context);

            var contextId = data["context"];

            if (string.IsNullOrWhiteSpace(contextId))
            {
                CloseResponse(context);

                return;
            }

            var mode = SearchResult.ParseMode(data["mode"]);

            var term = data["term"];

            if (string.IsNullOrWhiteSpace(term))
                term = string.Empty;

            string parentContextId;

            data.TryGetValue("parent", out parentContextId);

            CloseResponse(context);

            Search(contextId, term, mode, parentContextId, context);
        }

        static void Search(string contextId, string term, RenderMode mode, string parentContextID, p2pContext context)
        {
            var bContextId = Utils.AddressFromBase64String(contextId);

            CacheItem<SearchResult> searchItem = null;

            if (bContextId != null)
                searchItem = searchs.FirstOrDefault(x => Addresses.Equals(x.CachedValue.ContextId, bContextId, true));

            SearchResult search = null;

            if (searchItem == null)
            {

            }

            if (searchItem == null)
            {
                var bParentId = Utils.AddressFromBase64String(parentContextID);

                CacheItem<SearchResult> parentContext = null;

                if (bParentId != null)
                {
                    parentContext = searchs.FirstOrDefault(x => Addresses.Equals(x.CachedValue.ContextId, bParentId));

                    if (parentContext != null)
                        parentContext.Reset();
                }

                search = new SearchResult(bContextId, term, mode, context, parentContext == null ? null : parentContext.CachedValue);


                searchs.Add(search);
            }
            else
            {
                searchItem.Reset();

                search = searchItem.CachedValue;

                search.AddSearch(term, mode);
            }


        }

        static bool ProcessCache(p2pContext context)
        {
            var path = GetSegment(context.HttpContext, 2);

            var address = Utils.AddressFromBase64String(path);

            if (address != null)
                path = pParameters.webCache + "/" + path;
            else
                path = pParameters.webCache + context.HttpContext.Request.RawUrl;

            if (Directory.Exists(path))
            {
                path = Path.Combine(path, "index.html");

                keepAlive.Add(Path.Combine(context.HttpContext.Request.Url.AbsoluteUri, "index.html"));
            }

            if (File.Exists(path))
            {
                if (!Client.AnyPeer() && !path.Contains(pParameters.welcomeHome))
                    return false;

                switch (Path.GetExtension(path))
                {
                    case ".gif": context.HttpContext.Response.ContentType = "image/gif"; break;
                    case ".png": context.HttpContext.Response.ContentType = "image/png"; break;
                    case ".jpeg":
                    case ".jpg": context.HttpContext.Response.ContentType = "image/jpg"; break;
                    case ".svg": context.HttpContext.Response.ContentType = "image/svg+xml"; break;
                    case ".css": context.HttpContext.Response.ContentType = "text/css"; break;
                    case ".html":
                    case ".htm": context.HttpContext.Response.ContentType = "text/html"; break;
                    case ".js": context.HttpContext.Response.ContentType = "text/javascript"; break;
                    case ".pdf": context.HttpContext.Response.ContentType = "application/pdf"; break;
                    case ".exe": context.HttpContext.Response.ContentType = "application/octet-stream"; break;
                    case ".zip": context.HttpContext.Response.ContentType = "application/zip"; break;
                    case ".doc": context.HttpContext.Response.ContentType = "application/msword"; break;
                    case ".xls": context.HttpContext.Response.ContentType = "application/vnd.ms-excel"; break;
                    case ".ppt": context.HttpContext.Response.ContentType = "application/vnd.ms-powerpoint"; break;

                    //default:context.HttpContext.Response.ContentType = "application/force-download"; break;// will break videos & audios7
                    default: context.HttpContext.Response.ContentType = "text/html"; break;
                }



                var d = new FileDownloadObject(address, context, path);

                if (address != null)
                    Client.Download(Utils.ToBase64String(address), context, pParameters.webCache + "/" + Utils.ToBase64String(address));

                ProcessSeek(context, new CacheItem<FileDownloadObject>(d));

                return true;
            }

            return false;
        }

        static void ProcessGet(p2pContext context)
        {

            var sAddress = GetSegment(context.HttpContext, 2);

            var address = Utils.AddressFromBase64String(sAddress);

            Log.Add(Log.LogTypes.WebServer, Log.LogOperations.Get, new { url = context.HttpContext.Request.Url.AbsoluteUri, address, Range = context.HttpContext.Request.Headers["Range"], context });

            keepAlive.Add(context.HttpContext.Request.Url.AbsoluteUri);

            if (ProcessCache(context))
                return;

            if (!Client.AnyPeer())
            {
                context.HttpContext.Response.Redirect("/" + pParameters.welcomeHome + "/");

                context.HttpContext.Response.Close();

                return;
            }

            if (address == null)
                return;

            var data = Client.GetLocal(address);

            //todo:pelamor só pra testar
            //data = null;

            if (data != null)
            {
                context.HttpContext.Response.OutputStream.Write(data, 0, data.Count());

                context.HttpContext.Response.Close();
            }
            else
            {
                FileDownloadObject ci = new FileDownloadObject(address, context, pParameters.webCache + "/" + Utils.ToBase64String(address));

                lock (downloads)
                    downloads.Add(ci);

                Client.Download(Utils.ToBase64String(address), context, pParameters.webCache + "/" + Utils.ToBase64String(address));

                ProcessSeek(context, new CacheItem<FileDownloadObject>(ci));

                //ci.packetArrivedEvent.WaitOne();

                //context.HttpContext.Response.Close();

                //context.HttpContext.Response.SendChunked = false;

                //Log.Add(Log.LogTypes.streamSeek, new { ondownload=1, File = ci.CachedValue, ci.CachedValue.Context.HttpContext.Response.Headers, Range = ci.CachedValue.Context.HttpContext.Request.Headers["Range"], RequestTraceIdentifier = ci.CachedValue.Context.HttpContext.Request.RequestTraceIdentifier });
            }

        }

        private static void ProcessSeek(p2pContext context, CacheItem<FileDownloadObject> cache)
        {
            Log.Add(Log.LogTypes.Stream, Log.LogOperations.Seek, new { File = cache.CachedValue, cache.CachedValue.Context.HttpContext.Response.Headers, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

            var download = cache.CachedValue;

            //var context = download.Context;

            // context.HttpContext.Response.Headers.Clear();

            var closeFile = download.FileStream == null;

            try
            {
                //if (!context.headerAlreadySent)
                //    context.HttpContext.Response.StatusCode = 206;


                long range1 = 0;

                long range2 = -1;

                try
                {
                    var ranges = context.HttpContext.Request.Headers["Range"].Substring(6).Split('-');

                    range1 = long.Parse(ranges[0]);

                    download.Context.OutputStreamBeginPosition = range1;


                    range2 = long.Parse(ranges[1]);

                    download.Context.OutputStreamEndPosition = range2;

                }
                catch { }


                if (closeFile)
                    download.FileStream = p2pStreamManager.GetStream(download.Filename, context, range1);

                if (download.FileStream.Length < 1)
                {
                    int retry = 3;

                    int count = 1;

                    while (download.FileStream.Length < 1)
                    {
                        Log.Add(Log.LogTypes.Stream, Log.LogOperations.CantSeek, new { File_Length = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

                        var address = download.Address;

                        download.packetArrivedEvent.Reset();

                        var packetArrived = download.packetArrivedEvent.WaitOne(pParameters.WebServer_FileDownloadTimeout / retry);

                        if (!packetArrived)
                        {
                            Log.Add(Log.LogTypes.Stream, Log.LogOperations.TimeOut, new { TRY = count, File_Length = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

                            if (count++ > retry)
                                return;
                        }

                        download.FileStream = p2pStreamManager.GetStream(download.Filename, context, range1);
                    }
                }



                download.Context.OutputStreamLength = download.FileStream.Length;

                if (range2 == -1)
                    range2 = download.FileStream.Length - 1;

                range2 = Math.Min(range2, range1 + pParameters.WebServer_MaxNonRangeDownloadSize - 1);

                download.Context.OutputStreamEndPosition = range2;

                var initial_position = range1;

                context.OutputStreamPosition = range1;

                long responseSize = (range2 + 1) - range1;

                //Log.Add(Log.LogTypes.Ever, new { HEADERS = 1, responseSize, context.headerAlreadySent, context.HttpContext.Response.Headers });


                if (!context.HttpContext.Response.Headers.HasKeys())
                //if (!context.headerAlreadySent)
                {
                    Log.Add(Log.LogTypes.Stream, Log.LogOperations.Header, new { context, context.HttpContext.Response.ContentLength64, responseSize, context.HttpContext.Response.Headers });


                    //if (download.FileStream.Filename.Contains("psP2xPTU") && range1 == 0)
                    //{
                    //    context.HttpContext.Response.ContentLength64 = responseSize - 5000;

                    //    context.HttpContext.Response.StatusCode = 206;

                    //    context.HttpContext.Response.AddHeader("Content-Length", (range2 - range1 - 5000).ToString());

                    //    context.HttpContext.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", range1, range2 - 5000, download.FileStream.Length - 5000));
                    //}
                    // else
                    {
                        context.HttpContext.Response.ContentType = "text/html";

                        context.HttpContext.Response.StatusCode = 206;

                        context.HttpContext.Response.ContentLength64 = responseSize;

                        context.HttpContext.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", range1, range2, download.FileStream.Length));

                        if (context.HttpContext.Request.Headers.AllKeys.Contains("Range") && range1 == 0)
                        {
                            download.FileStream.InitialLoadDone();

                            Log.Add(Log.LogTypes.Stream, Log.LogOperations.ClosingInitialRequest, new { File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], context });

                            context.HttpContext.Response.OutputStream.Flush();
                        }


                    }




                    if (download.FileStream.Filename.Contains("psP2xPTU"))
                        context.HttpContext.Response.AddHeader("Content-Type", "video/mp4");

                    context.headerAlreadySent = true;
                }

                long bufferSize = Math.Min(pParameters.WebServer_MaxNonRangeDownloadSize, responseSize);

                var buffer = new byte[bufferSize];

                var read = 0;

                try
                {

                    var retry = 3;

                    var count = 1;

                    while (true)
                    {

                        download.FileStream = p2pStreamManager.GetStream(download.Filename, context, context.OutputStreamPosition);

                        p2pFile.Packet[] packets = null;

                        read = download.FileStream.Read(buffer, (int)context.OutputStreamPosition, (int)bufferSize, context, out packets);

                        if (read == 0)
                            return;

                        if (read == -1)
                        {
                            Log.Add(Log.LogTypes.Stream, Log.LogOperations.CantSeek, new { TRY = count, File_Read = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

                            download.packetArrivedEvent.Reset();

                            foreach (var p in packets)
                                p.Get();

                            var packetArrived = download.packetArrivedEvent.WaitOne(packets.Any()? pParameters.WebServer_FileDownloadTimeout : 0);

                            if (!packetArrived)
                            {
                                Log.Add(Log.LogTypes.Stream, Log.LogOperations.TimeOut, new { TRY = count, File_Read = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

                                if (count++ > retry)
                                    return;
                                else
                                    continue;
                            }
                            else
                                continue;
                        }




#if DEBUG
                        //if (download.FileStream.Filename.Contains("psP2xPTU"))
                        //{
                        //    var original = File.ReadAllBytes(@"D:\lareda\windows_desktop\bin\Debug\temp\9LW5P-R7JY24CH72a_grrVMlZshJ3nJ1PkXxmzR74u8=.mp4");

                        //    var part = original.Skip((int)context.OutputStreamPosition).Take((int)bufferSize).ToArray();

                        //    if (!part.SequenceEqual(buffer))
                        //    {
                        //        var buf = System.Text.Encoding.Default.GetString(buffer.Take(read).ToArray());

                        //        var par = System.Text.Encoding.Default.GetString(part.Take(read).ToArray());

                        //        if (buf != par)
                        //        {

                        //        }
                        //    }
                        //}
#endif

                        OnFileWrite?.Invoke(download.FileStream.Filename, new int[] { Convert.ToInt32((long)(download.FileStream.GetPosition(context) / (32 * 2029))) });

                        var attempts = 0;

                        try
                        {
                            lock (context)
                                context.HttpContext.Response.OutputStream.Write(buffer, 0, read);

                            Log.Add(Log.LogTypes.Stream, Log.LogOperations.Write, new { File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], RequestTraceIdentifier = cache.CachedValue.Context.HttpContext.Request.RequestTraceIdentifier });
                        }
                        catch (Exception e)
                        {
                            Log.Add(Log.LogTypes.Stream, Log.LogOperations.Exception, new { e, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });
                        }

                        context.OutputStreamPosition += read;

                        context.HttpContext.Response.OutputStream.Flush();



                        if (context.HttpContext.Request.Headers.AllKeys.Contains("Range"))
                            return;

                    }
                }
                catch (HttpListenerException e)
                {
                    Log.Add(Log.LogTypes.WebServer, Log.LogOperations.Exception, e);
                }
                catch (Exception ex)
                {
                    Log.Add(Log.LogTypes.Stream, Log.LogOperations.Exception, new { ex, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });
                }
                finally
                {
                    Log.Add(Log.LogTypes.WebServer, Log.LogOperations.ClosingResponse, new { File = cache.CachedValue, Read = read, context.OutputStreamPosition, Range1 = range1, Range2 = range2, context });

                    context.HttpContext.Response.Close();
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.Add(Log.LogTypes.Stream, Log.LogOperations.Exception, ex);

                Log.Add(Log.LogTypes.Stream, Log.LogOperations.Exception, new { ex, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogTypes.Stream, Log.LogOperations.Exception, new { ex, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });
            }
            finally
            {
                Log.Add(Log.LogTypes.Stream, Log.LogOperations.ClosingContext, new { File = cache.CachedValue, context, CloseContext = closeFile && download.FileStream != null });

                if (closeFile && download.FileStream != null)
                    download.FileStream.Dispose(context);

                downloads.Remove(cache);
            }
        }


        private static void ProcessCreateUserAvatar(p2pContext context)
        {
            var name = GetSegment(context.HttpContext, 3);

            if (string.IsNullOrWhiteSpace(name))
            {
                CloseResponse(context);

                return;
            }

            var address = Client.CreateUser(name);

            var renderer = new IdenticonGenerator("MD5", new Size(240, 240), Color.FromArgb(100, address[0], address[1], address[2]));

            using (Bitmap bmp = renderer.Create(address))
            {
                var path = Path.Combine(pParameters.webCache, address + ".png");

                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);

                    var data = new byte[ms.Length];

                    ms.Seek(0, 0);

                    ms.Read(data, 0, data.Length);

                    Client.PostImage(address, data);


                    //todo: refazer post com packet e nao metapacket
                    //var post = Metapacket.Create(data, null, bAddress);
                    //var post = p2pRequest.bytes_empty;

                    //Metapacket.Create(post, VirtualAttributes.MIME_TYPE_IMAGE);

                    //Metapacket.Create(bAddress, post);
                }
            }

            CloseResponse(context, Utils.ToBase64String(address));

        }

        private static void ProcessAddPeer(p2pContext context)
        {
            var key = context.HttpContext.Request.QueryString["key"];

            Client.GetPeer(key);

            context.HttpContext.Response.Redirect("/" + pParameters.webHome + "/");

            context.HttpContext.Response.Close();
        }

        private static void ProcessKeepAlive(p2pContext context)
        {
            var files = JsonConvert.DeserializeObject<string[]>(context.HttpContext.Request.QueryString["files"]);

            foreach (var f in files)
                keepAlive.Add(f);

            context.HttpContext.Response.Close();
        }



        static void ProcessDrag(p2pContext context, string command)
        {
            var dragId = GetSegment(context.HttpContext, 3);

            if (string.IsNullOrWhiteSpace(dragId))
            {
                CloseResponse(context);

                return;
            }

            var userAddress = GetSegment(context.HttpContext, 4);

            OnDragging?.Invoke(command == WebCommands.DragOver, dragId, userAddress);

            context.HttpContext.Response.Close();
        }

        static void ProcessPost(p2pContext context)
        {
            var data = ProcessForm(context);

            var post = string.Empty;

            var target = string.Empty;

            var user = string.Empty;


            if (!data.TryGetValue("post", out post) ||
                !data.TryGetValue("user", out user) ||
                user == null)
            {
                CloseResponse(context);

                return;
            }

            data.TryGetValue("target", out target);

            var address = Client.Post(post, null, target, user);

            CloseResponse(context, Utils.ToBase64String(address));
        }

        static void Client_onSearchReturn(byte[] search, Metapacket[] posts)
        {

            //List<CacheItem<p2pResultItem>> items = null;

            //List<CacheItem<SearchResult>> results;

            //lock (searchs)
            //    results = searchs.FindAll(x => x.CachedValue.Searched(search));

            //foreach(var r in results)
            //    r.CachedValue.a

            //    searchs[ .AddResults(term, posts);
            //}

        }

        static void Client_OnFileDownload(byte[] address, string filename, string speficFilename, int[] arrives, int[] cursors)
        {
            //return;

            CacheItem<FileDownloadObject>[] download_items = null;

            lock (downloads)
            {
                download_items = downloads.Where(x => x.CachedValue.Address != null && Addresses.Equals(x.CachedValue.Address, address, true)).ToArray();

                //foreach (CacheItem<FileDownloadObject> c in download_items)
                //    downloads.Remove(c);
            }


            lock (download_items)
                foreach (var cache in download_items)
                {
                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.Complete, cache);

                    cache.CachedValue.packetArrivedEvent.Set();

                    //ProcessSeek(cache);
                }
        }

        static void CloseResponse(p2pContext context, string result = null)
        {
            if (result != null)
            {
                // var json = JsonConvert.SerializeObject(result);

                var buffer = context.HttpContext.Request.ContentEncoding.GetBytes(result);

                context.HttpContext.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            context.HttpContext.Response.Close();
        }

        static string GetSegment(HttpListenerContext context, int segment)
        {
            if (context.Request.Url.Segments.Length <= segment)
                return null;

            return removeLastSlash(context.Request.Url.Segments[segment]);
        }

        static string removeLastSlash(string value)
        {
            if (value[value.Length - 1] == '/')
                return value.Substring(0, value.Length - 1);

            return value;
        }

        class WebCommands
        {
            internal const string Search = "search";

            internal const string Context = "context";

            internal const string DragOver = "dragover";

            internal const string DragLeave = "dragleave";

            internal const string FileUpload = "fileUpload";

            internal const string Post = "post";

            internal const string AddPeer = "addPeer";

            internal const string FriendDownload = "friendDownload";

            internal const string FriendKey = "friendKey";

            internal const string CreateUserAvatar = "createUserAvatar";

            internal const string KeepAlive = "keepAlive";
        }


    }
}



