using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace library
{

    public class p2pContext
    {
        public HttpListenerContext HttpContext;

        public bool headerAlreadySent = false;

        internal long OutputStreamPosition
        {
            get
            {
                if (Download == null || Download.FileStream == null)
                    return 0;

                return Download.FileStream.GetPosition(Download.Source);
            }
        }

        public FileDownloadObject Download = null;

        internal Stream OutputStream
        {
            get { return HttpContext.Response.OutputStream; }
        }


        public p2pContext(HttpListenerContext httpContext)
        {
            HttpContext = httpContext;
        }

    }
}
