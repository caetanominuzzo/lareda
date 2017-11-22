using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace library
{

    public class p2pContext : IDisposable
    {
        public byte[] ContextId = null;

        [JsonIgnore]
        public HttpListenerContext HttpContext;

        public bool DelayedWrite = false;

        public bool headerAlreadySent = false;

        public long OutputStreamBeginPosition = 0;

        public long OutputStreamEndPosition = -1;

        public long OutputStreamLength = 0;

        public long OutputStreamPosition = 0;

        public FileDownloadObject Download = null;

        [JsonIgnore]
        internal Stream OutputStream
        {
            get { return HttpContext.Response.OutputStream; }
        }


        public p2pContext(HttpListenerContext httpContext = null, bool delayedWrite = false)
        {
            HttpContext = httpContext;

            DelayedWrite = delayedWrite;
        }

        public void Dispose()
        {
            try
            {
                this.HttpContext.Response.OutputStream.Dispose();
            }
            catch { }

            try
            {
                this.HttpContext.Response.Close();
            }
            catch { }

            if (null != Download)
                Download.Dispose();
        }
    }
}
