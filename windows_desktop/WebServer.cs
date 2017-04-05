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

        static HttpListener server;

        public delegate void DraggingHandler(bool dragging, string dragId, string userAddress);

        static public event DraggingHandler OnDragging;

        static Cache<FileDownloadObject> downloads = new Cache<FileDownloadObject>(120000);

        static Cache<SearchResult> searchs = new Cache<SearchResult>(60000);

        #region Thread Refresh

        static Thread thread;

        static bool stop = false;

        internal static IDisposable Start()
        {
            //todo: pra fechar a conexão
            //searchs.OnCacheExpired += searchs_OnCacheExpired;

            if (thread == null)
            {
                thread = new Thread(Configure);

                thread.Start();
            }


            return new WebServer();
        }

        public void Dispose()
        {
            stop = true;

            server.Stop();
        }

        public static void Configure()
        {
            //Client.OnSearchReturn += Client_onSearchReturn;

            Client.OnFileDownload += Client_OnFileDownload;

            server = new HttpListener();

            server.Prefixes.Add("http://+:" + Program.WebPort + "/");


            try
            {
                server.Start();
            }
            catch (HttpListenerException e)
            {
                if (Program.RunAsAdministrator(AppDomain.CurrentDomain.FriendlyName, "NETSH"))
                {
                    Configure();
                    return;
                }
            }

            ThreadReceive();
        }

        static void ThreadReceive()
        {
            while (!stop)
            {
                try
                {
                    if (server.IsListening)
                        ThreadPool.QueueUserWorkItem(ProcessReceive, new p2pContext(server.GetContext()));

                    //ProcessReceive(server.GetContext());
                }
                catch { }
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

            response.AddHeader("Access-Control-Allow-Origin", "*");

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
                path = Path.Combine(path, "index.html");

            if (File.Exists(path))
            {
                if (!Client.AnyPeer() && !path.Contains(Program.welcomeHome))
                    return false;

                var d = new FileDownloadObject(address, context, path);

                ProcessSeek(new CacheItem<FileDownloadObject>(d));

                return true;
            }

            return false;
        }

        static void ProcessGet(p2pContext context)
        {
            if (ProcessCache(context))
                return;

            if (!Client.AnyPeer())
            {
                context.HttpContext.Response.Redirect("/" + Program.welcomeHome + "/");

                context.HttpContext.Response.Close();

                return;
            }

            //var sAddress = HttpUtility.UrlDecode(GetSegment(context, 2));

            var sAddress = GetSegment(context.HttpContext, 2);

            var address = Utils.AddressFromBase64String(sAddress);

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
                var ci = downloads.Add(new FileDownloadObject(address, context, Program.webCache + "/" + Utils.ToBase64String(address)));

                Client.Download(Utils.ToBase64String(address), context, Program.webCache + "/" + Utils.ToBase64String(address));
            }



            #region old 

            //var address = Utils.FromBase64String(command);

            //if (address != null)
            //{
            //    filename = Path.Combine(Program.webCache, command);

            //    if (path != "index.html" || context.Request.QueryString["type"] != null)
            //    {
            //        if (context.Request.QueryString["type"] == "download")
            //        {
            //            filename = context.Request.QueryString["name"];

            //            filename = Program.UIHelper.SaveAs(filename);

            //            if (!string.IsNullOrEmpty(filename))
            //                Client.Download(command, filename);

            //            context.Response.Close();

            //            return;
            //        }
            //        else if (commands.Length > 1 || context.Request.QueryString["type"] == "web")
            //        {
            //            if (!string.IsNullOrEmpty(filename))
            //            {
            //                Client.Download(command, Path.Combine(Program.webCache, command), path);

            //                getResults.Add(new GetReturnObject(command, context, path));
            //            }

            //            return;
            //        }

            //        context.Response.ContentType = context.Request.QueryString["type"];
            //    }
            //    else
            //        context.Response.ContentType = "application/octet-stream";

            //    if (context.Request.Headers["Range"] != null || context.Request.Headers["If-None-Match"] != null)
            //        ProcessSeek(context, command);

            //    byte[] buffer = Client.GetPost(address);

            //    if (buffer != null)
            //    {
            //        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            //    }
            //    else
            //    {
            //        getResults.Add(new GetReturnObject(command, context));

            //        Client.Download(command, filename);

            //        return;
            //    }

            //    context.Response.Close();
            //}

            #endregion
        }

        private static void ProcessSeek(CacheItem<FileDownloadObject> cache)
        {

            var download = cache.CachedValue;

            var context = download.Context;



            var closeFile = download.FileStream == null;

            var source = download.Source;

            var waitMoreData = false;

            try
            {

                long range1 = 0;

                long range2 = -1;

                try
                {
                    var ranges = context.HttpContext.Request.Headers["Range"].Substring(6).Split('-');

                    range1 = long.Parse(ranges[0]);

                    if (!context.headerAlreadySent)
                        context.HttpContext.Response.StatusCode = 206;

                    range2 = long.Parse(ranges[1]);


                }
                catch { }

                if (closeFile)
                    download.FileStream = p2pStream.GetStream(download.Filename, source, range1);

                var file = download.FileStream;

                if (file.Length == -1)
                {
                    Log.Add(Log.LogTypes.Ever, new { READ_MENOS_UM = 1, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, Position = file.GetPosition(source), file });

                    var address = download.Address;

                    var ci = downloads.Add(new FileDownloadObject(address, context, Program.webCache + "/" + Utils.ToBase64String(address)));

                    Client.Download(Utils.ToBase64String(address), context, Program.webCache + "/" + Utils.ToBase64String(address));

                    waitMoreData = true;

                    return;
                }



                if (range2 == -1)
                    range2 = file.Length - 1;


                long position = file.GetPosition(source);

                if (!context.headerAlreadySent)
                    context.HttpContext.Response.StatusCode = 206;





                long responseSize = (range2 + 1) - range1;

                //Log.Add(Log.LogTypes.Ever, new { HEADERS = 1, responseSize, context.headerAlreadySent, context.HttpContext.Response.Headers });

                Log.Add(Log.LogTypes.streamSeek, new { File = cache.CachedValue, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier });

                if (!context.headerAlreadySent)
                {
                    context.HttpContext.Response.AddHeader("Content-Length", (range2 - range1).ToString());

                    context.HttpContext.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", range1, range2, file.Length)); ;

                    context.HttpContext.Response.ContentLength64 = responseSize;

                    context.headerAlreadySent = true;
                }


                //file.Seek(range1, 0, source);

                long bufferSize = Math.Min(Program.MaxNonRangeDownloadSize, responseSize);

                var buffer = new byte[bufferSize];

                var read = 0;



                try
                {


                    while (true)
                    {
                        read = file.Read(buffer, 0, (int)bufferSize, source);

                        if (read == 0)
                            break;

                        if (read == -1)
                        {
                            Log.Add(Log.LogTypes.Ever, new { READ_MENOS_UM = 1, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, Position = file.GetPosition(source), file });

                            var address = download.Address;

                            var ci = downloads.Add(new FileDownloadObject(address, context, Program.webCache + "/" + Utils.ToBase64String(address)));

                            Client.Download(Utils.ToBase64String(address), context, Program.webCache + "/" + Utils.ToBase64String(address));

                            waitMoreData = true;

                            break;
                        }

                        var attempts = 0;

                        while(attempts < 1)
                        {
                            try
                            {
                                lock (context)
                                    context.HttpContext.Response.OutputStream.Write(buffer, 0, read);

                                  break;
                            }
                            catch (HttpListenerException e)
                            {
                                Log.Add(Log.LogTypes.Ever, new { Exception_ZERO = 1, attempts, File = cache.CachedValue, Read = read, Position = position, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, Exception = e });

                                Thread.Sleep(1);
                            }

                            attempts++;
                        }

                        //context.Response.OutputStream.Flush();

                        Log.Add(Log.LogTypes.streamWrite, new { File = cache.CachedValue, Read = read, Position = position, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier });

                        position += read;

                        cache.Reset();
                    }
                }

                catch (HttpListenerException e)
                {
                    waitMoreData = false;

                    Log.Add(Log.LogTypes.Ever, new { Exception_Um = 1, File = cache.CachedValue, Read = read, Position = position, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, Exception = e });
                }
                finally
                {
                    Log.Add(Log.LogTypes.streamOutputClose, new { File = cache.CachedValue, Read = read, Position = position, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, CloseStream = !waitMoreData });

                    if (!waitMoreData)
                        context.HttpContext.Response.Close();
                }
            }
            catch (ObjectDisposedException e)
            {
                Log.Add(Log.LogTypes.Ever, new { Exception_Um = 2, File = cache.CachedValue, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, Exception = e });
            }
            catch (HttpListenerException e)
            {
                Log.Add(Log.LogTypes.Ever, new { Exception_Um = 3, File = cache.CachedValue, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, Exception = e });
            }
            finally
            {
                Log.Add(Log.LogTypes.streamInputClose, new { File = cache.CachedValue, RequestTraceIdentifier = context.HttpContext.Request.RequestTraceIdentifier, CloseStream = closeFile && download.FileStream != null && !waitMoreData });

                if (closeFile && download.FileStream != null && !waitMoreData)
                {
                    download.FileStream.Dispose(source);
                }

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

        static void ProcessDrag(p2pContext context, string command)
        {
            var dragId = GetSegment(context.HttpContext, 3);

            if (string.IsNullOrWhiteSpace(dragId))
            {
                CloseResponse(context);

                return;
            }

            var userAddress = GetSegment(context.HttpContext, 4);

            if (OnDragging != null)
                OnDragging(command == WebCommands.DragOver, dragId, userAddress);

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

        static void Client_OnFileDownload(byte[] address, string filename, string speficFilename = null)
        {
            //return;

            CacheItem<FileDownloadObject>[] download_items = null;

            lock (downloads)
            {
                download_items = downloads.Where(x => x.CachedValue.Address != null && Addresses.Equals(x.CachedValue.Address, address, true)).ToArray();

                foreach (CacheItem<FileDownloadObject> c in download_items)
                    downloads.Remove(c);
            }


            lock (download_items)
                foreach (var cache in download_items)
                {
                    if (cache.CachedValue.Filename.Contains("jBx-OWAanol4XmecG1R9hqLDVIrfHDllnS9vwBnejy0="))
                        Log.Write("OnDownload\t" + cache.CachedValue.Context.HttpContext.Request.RequestTraceIdentifier);

                    ProcessSeek(cache);
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
        }


    }
}



