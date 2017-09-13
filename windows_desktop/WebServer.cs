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
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;

namespace windows_desktop
{

    public class WebServer : IDisposable
    {
        internal static ManualResetEvent stopEvent = new ManualResetEvent(false);

        public delegate void DraggingHandler(bool dragging, string dragId, string userAddress);

        static public event DraggingHandler OnDragging;

        internal static Cache<FileDownloadObject> downloads = new Cache<FileDownloadObject>(2100000000);

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

            if (thread == null)
            {
                thread = new Thread(Configure);

                thread.Start();
            }


            return new WebServer();
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

        public static void Configure()
        {
            //Client.OnSearchReturn += Client_onSearchReturn;

            Client.OnFileDownload += Client_OnFileDownload;

            try
            {

                using (var server = new Unosquare.Labs.EmbedIO.WebServer("http://+:" + Program.WebPort + "/", RoutingStrategy.Regex))
                {
                    server.RegisterModule(new FallbackModule(ThreadReceiveNew));

                    server.RunAsync();

                    stopEvent.WaitOne();
                }


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

        static bool ThreadReceiveNew(HttpListenerContext context, CancellationToken cancel)
        {
            ProcessReceive(new p2pContext(context));

            //context.JsonResponse(new { Hola = "Message" });

            return true;
        }

        //static void ThreadReceive()
        //{
        //    while (!stop)
        //    {
        //        try
        //        {
        //            if (server.IsListening)
        //            {
        //                IAsyncResult result = server.BeginGetContext(new AsyncCallback(ListenerCallback), server);

        //                result.AsyncWaitHandle.WaitOne();

        //                //var c = new p2pContext(server.GetContext());

        //                //Log.Write("CREATE" + c.ToString());

        //                //ThreadPool.QueueUserWorkItem(ProcessReceive, c);

        //                //Program.GetThreads();
        //            }

        //            // ProcessReceive(new p2pContext(server.GetContext()));
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    }
        //}


        public static void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;

            HttpListenerContext context = listener.EndGetContext(result);

            using (var c = new p2pContext(context))
            {
                ProcessReceive(c);
            }

            //Log.Write("CREATE" + c.ToString());

            //ThreadPool.QueueUserWorkItem(ProcessReceive, c);
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
                response.RedirectLocation = Program.webHome + "/index.html";

                response.Redirect("/" + Program.webHome + "/");

                //context.Response.StatusCode = (int)HttpStatusCode.Ambiguous;

                CloseResponse(context);

                return;
            }

            var command = string.Empty;

            if (baseAddress == Program.webHome || baseAddress == Program.welcomeHome)
                command = GetSegment(context.HttpContext, 2);

            switch (command)
            {
#if DEBUG
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


#else
              default:

                    ProcessGet(context);

                    break;

#endif
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
                path = Program.webCache + "/" + path;
            else
                path = Program.webCache + context.HttpContext.Request.RawUrl;

            if (Directory.Exists(path))
            {
                path = Path.Combine(path, "index.html");

                keepAlive.Add(Path.Combine(context.HttpContext.Request.Url.AbsoluteUri, "index.html"));
            }

            if (File.Exists(path))
            {
                if (!Client.AnyPeer() && !path.Contains(Program.welcomeHome))
                    return false;

                var d = new FileDownloadObject(address, context, path);

                if(address != null)
                    Client.Download(Utils.ToBase64String(address), context, Program.webCache + "/" + Utils.ToBase64String(address));

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
                context.HttpContext.Response.Redirect("/" + Program.welcomeHome + "/");

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
                //var json = JsonConvert.SerializeObject(new { A = sAddress, C = Encoding.Unicode.GetString(data) });

                //data = context.Request.ContentEncoding.GetBytes(json);

                context.HttpContext.Response.OutputStream.Write(data, 0, data.Count());

                context.HttpContext.Response.Close();
            }
            else
            {
                FileDownloadObject ci = new FileDownloadObject(address, context, Program.webCache + "/" + Utils.ToBase64String(address));

                lock (downloads)
                    downloads.Add(ci);

                Client.Download(Utils.ToBase64String(address), context, Program.webCache + "/" + Utils.ToBase64String(address));

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

            if (download.Filename.Contains("gLseAGI6QBqGWA5YKLUIHEoU"))
            {

            }

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

                var file = download.FileStream;

                while (file.Length < 1)
                {


                    Log.Add(Log.LogTypes.Stream, Log.LogOperations.CantSeek, new { File_Length = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

                    var address = download.Address;

                    var packetArrived = download.packetArrivedEvent.WaitOne(pParameters.WebServer_FileDownloadTimeout);

                    if (!packetArrived)
                    {
                        Log.Add(Log.LogTypes.Stream, Log.LogOperations.TimeOut, new { File_Length = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });


                        return;
                    }
                }

                download.Context.OutputStreamLength = file.Length;

                if (range2 == -1)
                {
                    range2 = file.Length - 1;

                    download.Context.OutputStreamEndPosition = range2;
                }

                context.OutputStreamPosition = range1;

                long responseSize = (range2 + 1) - range1;

                //Log.Add(Log.LogTypes.Ever, new { HEADERS = 1, responseSize, context.headerAlreadySent, context.HttpContext.Response.Headers });


                if (!context.HttpContext.Response.Headers.HasKeys())
                //if (!context.headerAlreadySent)
                {
                    Log.Add(Log.LogTypes.Stream, Log.LogOperations.Header, new { context, context.HttpContext.Response.ContentLength64, responseSize, context.HttpContext.Response.Headers });

                    context.HttpContext.Response.ContentLength64 = responseSize;

                    context.HttpContext.Response.StatusCode = 206;

                    context.HttpContext.Response.AddHeader("Content-Length", (range2 - range1).ToString());

                    context.HttpContext.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", range1, range2, file.Length)); ;


                    if (file.Filename.Contains("psP2xPTU"))
                        context.HttpContext.Response.AddHeader("Content-Type", "video/mp4");

                    context.headerAlreadySent = true;
                }

                long bufferSize = Math.Min(Program.MaxNonRangeDownloadSize, responseSize);

                var buffer = new byte[bufferSize];

                var read = 0;

                try
                {

                    var wrote = 0;

                    while (true)
                    {
                        lock (keepAlive)
                            if (!keepAlive.Any(x => Addresses.Equals(x.CachedValue, cache.CachedValue.Context.HttpContext.Request.Url.AbsoluteUri)))
                            {
                                //waitMoreData = false;

                                //return;
                            }

                        read = file.Read(buffer, 0, (int)bufferSize, context);

                        if (read == 0)
                            return;

                        if (read == -1)
                        {
                            Log.Add(Log.LogTypes.Stream, Log.LogOperations.CantSeek, new { File_Read = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

                            var packetArrived = download.packetArrivedEvent.WaitOne(pParameters.WebServer_FileDownloadTimeout);

                            if (!packetArrived)
                            {
                                Log.Add(Log.LogTypes.Stream, Log.LogOperations.TimeOut, new { File_Read = -1, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });

                                return;
                            }
                            else
                                continue;
                        }

                        OnFileWrite?.Invoke(file.Filename, new int[] { Convert.ToInt32((long)(file.GetPosition(context) / (32 * 2029))) });

                        var attempts = 0;

                        while (attempts < 1)
                        {
                            try
                            {
                                using (var timer = new System.Threading.Timer(_ =>
                                {
                                    Log.Add(Log.LogTypes.Stream, Log.LogOperations.TimeOut, new { Write = true, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], context });

                                    context.HttpContext.Response.Abort();

                                }, null, 1000, Timeout.Infinite))
                                {



                                    lock (context)
                                        context.HttpContext.Response.OutputStream.Write(buffer, 0, read);
                                    timer.Dispose();
                                    }

                                Log.Add(Log.LogTypes.Stream, Log.LogOperations.Write, new { File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], RequestTraceIdentifier = cache.CachedValue.Context.HttpContext.Request.RequestTraceIdentifier });

                                break;
                            }
                            catch (Exception e)
                            {
                                Log.Add(Log.LogTypes.Stream, Log.LogOperations.Exception, new { e, File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], cache.CachedValue.Context });
                            }

                            attempts++;
                        }

                        context.HttpContext.Response.OutputStream.Flush();

                        if (context.HttpContext.Request.Headers.AllKeys.Contains("Range") && range1 == 0)
                        {
                            Log.Add(Log.LogTypes.Stream, Log.LogOperations.ClosingInitialRequest, new { File = cache.CachedValue, Range = cache.CachedValue.Context.HttpContext.Request.Headers["Range"], context });

                            return;
                        }

                        context.OutputStreamPosition += read;

                        cache.Reset();

                    }
                }
                catch (HttpListenerException e)
                {
                    Log.Add(Log.LogTypes.WebServer, Log.LogOperations.Exception, e);
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
            }
            finally
            {
                Log.Add(Log.LogTypes.Stream, Log.LogOperations.ClosingContext, new { File = cache.CachedValue, context, CloseContext = closeFile && download.FileStream != null });

                if (closeFile && download.FileStream != null)
                    download.FileStream.Dispose(context);
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
                var path = Path.Combine(Program.webCache, address + ".png");

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

            context.HttpContext.Response.Redirect("/" + Program.webHome + "/");

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



