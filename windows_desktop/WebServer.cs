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

        static Cache<FileDownloadObject> downloads = new Cache<FileDownloadObject>(60000);

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
                        ThreadPool.QueueUserWorkItem(ProcessReceive, server.GetContext());

                    //ProcessReceive(server.GetContext());
                }
                catch { }
            }
        }



        #endregion

        static void ProcessReceive(object o)
        {
            var context = (HttpListenerContext)o;

            if (context.Request.RawUrl.Contains("favico"))
            {
                CloseResponse(context);

                return;
            }

            var response = context.Response;

            context.Response.AddHeader("Access-Control-Allow-Origin", "*");

            var baseAddress = GetSegment(context, 1);

            if (baseAddress == null || Utils.AddressFromBase64String(baseAddress) == null)
            {
                context.Response.RedirectLocation = Program.webHome + "/index.html";

                context.Response.Redirect("/" + Program.webHome + "/");

                //context.Response.StatusCode = (int)HttpStatusCode.Ambiguous;

                CloseResponse(context);

                return;
            }

            var command = string.Empty;

            if (baseAddress == Program.webHome || baseAddress == Program.welcomeHome)
                command = GetSegment(context, 2);

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

        static void ProcessContext(HttpListenerContext context)
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

        static Dictionary<string, string> ProcessForm(HttpListenerContext context)
        {
            var result = new Dictionary<string, string>();

            string content;

            using (var reader = new StreamReader(context.Request.InputStream,
                                     context.Request.ContentEncoding))
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

        static void ProcessSearch(HttpListenerContext context)
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

        static void Search(string contextId, string term, RenderMode mode, string parentContextID, HttpListenerContext context)
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

        static bool ProcessCache(HttpListenerContext context)
        {
            var path = GetSegment(context, 2);

            var address = Utils.AddressFromBase64String(path);

            if (address != null)
                path = Program.webCache + "/" + path;
            else
                path = Program.webCache + context.Request.RawUrl;

            if (Directory.Exists(path))
                path = Path.Combine(path, "index.html");

            if (File.Exists(path))
            {
                if (!Client.AnyPeer() && !path.Contains(Program.welcomeHome))
                    return false;


                //todo: nao adicionar cache nos downloads, mas somente no seek, mas entao como?
                //    if (address != null)
                {
                    //var t = new CacheItem<FileDownloadObject>

                    ProcessSeek(downloads.Add(new FileDownloadObject(address, context, path)));

                    return true;
                }


                byte[] buffer = File.ReadAllBytes(path);

                context.Response.ContentType = Utils.GetMimeType(path); // "text/html";

                context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                context.Response.Close();

                return true;
            }

            return false;
        }

        static void ProcessGet(HttpListenerContext context)
        {
            if (ProcessCache(context))
                return;

            if (!Client.AnyPeer())
            {
                context.Response.Redirect("/" + Program.welcomeHome + "/");

                context.Response.Close();

                return;
            }

            //var sAddress = HttpUtility.UrlDecode(GetSegment(context, 2));

            var sAddress = GetSegment(context, 2);

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

                context.Response.OutputStream.Write(data, 0, data.Count());

                context.Response.Close();
            }
            else
            {
                downloads.Add(new FileDownloadObject(address, context, Program.webCache + "/" + Utils.ToBase64String(address)));

                if (address[0] == 246 && address[1] == 171)
                {

                }

                //precisa setar alguma coisa no response pra ele nao morrer.
                context.Response.StatusCode = 666;

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

            try
            {

                if (closeFile)
                {
                    //download.FileStream = new FileStream(download.Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    download.FileStream = p2pStream.GetStream(download.Filename, source);

                    //download.FileStream.Seek(0, SeekOrigin.Begin);
                }

                var file = download.FileStream;

                long position = file.GetPosition(source);

                context.Response.StatusCode = 200;

                long range1 = 0;

                long range2 = file.Length - 1;

                try
                {
                    var ranges = context.Request.Headers["Range"].Substring(6).Split('-');

                    range1 = long.Parse(ranges[0]);

                    context.Response.StatusCode = 206;

                    range2 = long.Parse(ranges[1]);


                }
                catch { }

                Log.Add(Log.LogTypes.streamSeek, new { File = cache.CachedValue, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.Request.RequestTraceIdentifier });

                context.Response.AddHeader("Content-Length", (range2 - range1).ToString());

                context.Response.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", range1, range2, file.Length)); ;

                long responseSize = (range2 + 1) - range1;

                context.Response.ContentLength64 = responseSize;

                file.Seek(range1, 0, source);

                long bufferSize = Math.Min(Program.MaxNonRangeDownloadSize, responseSize);

                var buffer = new byte[bufferSize];

                var read = 0;

                var waitMoreData = false;

                try
                {


                    while (true)
                    {
                        read = file.Read(buffer, 0, (int)bufferSize, source);

                        if (read == 0)
                            break;

                        if (read == -1)
                        {
                            waitMoreData = true;

                            break;
                        }

                        lock(context)
                            context.Response.OutputStream.Write(buffer, 0, read);

                        //context.Response.OutputStream.Flush();

                        Log.Add(Log.LogTypes.streamWrite, new { File = cache.CachedValue, Read = read, Position = position, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.Request.RequestTraceIdentifier });

                        position += read;

                        cache.Reset();
                    }
                }
                catch (HttpListenerException e)
                {
                    Log.Add(Log.LogTypes.Ever, new { File = cache.CachedValue, Read = read, Position = position, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.Request.RequestTraceIdentifier, Exception = e });
                }
                finally
                {
                    Log.Add(Log.LogTypes.streamOutputClose, new { File = cache.CachedValue, Read = read, Position = position, Range1 = range1, Range2 = range2, RequestTraceIdentifier = context.Request.RequestTraceIdentifier, CloseStream = !waitMoreData });

                    if (!waitMoreData)
                        context.Response.Close();
                }
            }
            finally
            {
                Log.Add(Log.LogTypes.streamInputClose, new { File = cache.CachedValue, RequestTraceIdentifier = context.Request.RequestTraceIdentifier, CloseStream = closeFile && download.FileStream != null });

                if (closeFile && download.FileStream != null)
                    download.FileStream.Dispose(source);
            }
        }


        private static void ProcessCreateUserAvatar(HttpListenerContext context)
        {
            var name = GetSegment(context, 3);

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

        private static void ProcessAddPeer(HttpListenerContext context)
        {
            var key = context.Request.QueryString["key"];

            Client.GetPeer(key);

            context.Response.Redirect("/" + Program.webHome + "/");

            context.Response.Close();
        }

        static void ProcessDrag(HttpListenerContext context, string command)
        {
            var dragId = GetSegment(context, 3);

            if (string.IsNullOrWhiteSpace(dragId))
            {
                CloseResponse(context);

                return;
            }

            var userAddress = GetSegment(context, 4);

            if (OnDragging != null)
                OnDragging(command == WebCommands.DragOver, dragId, userAddress);

            context.Response.Close();
        }

        static void ProcessPost(HttpListenerContext context)
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

            FileDownloadObject download = null;

            CacheItem<FileDownloadObject> cache = null;

            lock (downloads)
            {
                cache = downloads.FirstOrDefault(x => x.CachedValue.Address != null && Addresses.Equals(x.CachedValue.Address, address, true));

                if (cache != null)
                {
                    download = cache.CachedValue;

                    downloads.Remove(cache);
                }
            }


            if (download != null)
            {
                if (cache.CachedValue.Filename.Contains("jBx-OWAanol4XmecG1R9hqLDVIrfHDllnS9vwBnejy0="))
                    Log.Write("OnDownload\t" + download.Context.Request.RequestTraceIdentifier);

                ProcessSeek(cache);

                return;

                //var g = Client.GetLocal(address);

                //if (g != null && download.Context.Response.OutputStream!= null && download.Context.Response.OutputStream.CanWrite)
                //    download.Context.Response.OutputStream.Write(g, 0, g.Count());

                //download.FileStream.CopyToAsync(download.Context.Response.OutputStream);
            }

            //download.Context.Response.Close();

            //CacheItem<GetReturnObject> result;

            //var base64Address = Utils.ToBase64String(address);

            //lock (getResults)
            //    result = getResults.FirstOrDefault(x => x.CachedValue.Address.Equals(base64Address));

            //if (result == null)
            //    return;

            ////var filename = Path.Combine(@"C:\arede\windows-desktop\bin\Debug\www\", result.CachedValue.Address);

            //if (!string.IsNullOrEmpty(speficFilename))
            //    filename = Path.Combine(filename, speficFilename);

            //try
            //{
            //    FileInfo file = new FileInfo(filename);

            //    if (file.Length <= Program.MaxNonRangeDownloadSize)
            //    {
            //        var buffer = File.ReadAllBytes(filename);

            //        if (result.CachedValue.Context.Response.OutputStream.CanWrite)
            //            result.CachedValue.Context.Response.OutputStream.Write(buffer, 0, buffer.Length);

            //        result.CachedValue.Context.Response.Close();
            //    }
            //    else
            //    {
            //        var seek = new FileDownloadObject(base64Address, new FileStream(filename, FileMode.Open, FileAccess.Read));

            //        downloads.Add(seek);

            //        var read = 0;

            //        var buffer = new byte[Program.MaxNonRangeDownloadSize];

            //        while ((read = seek.FileStream.Read(buffer, 0, buffer.Length)) > 0)
            //        {
            //            result.CachedValue.Context.Response.OutputStream.Write(buffer, 0, read);

            //            result.CachedValue.Context.Response.OutputStream.Flush();
            //        }

            //        result.CachedValue.Context.Response.Close();

            //    }
            //}
            //catch { }

            //result.Expire();

        }



        static void CloseResponse(HttpListenerContext context, string result = null)
        {
            if (result != null)
            {
                // var json = JsonConvert.SerializeObject(result);

                var buffer = context.Request.ContentEncoding.GetBytes(result);

                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            context.Response.Close();
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

        class FileDownloadObject : IDisposable
        {
            internal byte[] Address;

            internal p2pStream FileStream;

            internal HttpListenerContext Context;

            public string Filename;

            internal string Source;

            internal FileDownloadObject(byte[] address, HttpListenerContext context, string filename, p2pStream fileStream = null)
            {
                Address = address;

                FileStream = fileStream;

                Filename = filename;

                Context = context;

                Source = Utils.ToBase64String(Utils.GetAddress());
            }

            public void Dispose()
            {
                FileStream.Dispose(Source);
            }
        }
    }
}



