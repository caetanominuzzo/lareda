using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class FileDownloadObject : IDisposable
    {
        public byte[] Address;

        [JsonIgnore]
        public p2pStream FileStream;

        [JsonIgnore]
        public p2pContext Context;

        public string Filename;

        public String Source;

        public FileDownloadObject(byte[] address, p2pContext context, string filename, p2pStream fileStream = null)
        {
            Address = address;

            FileStream = fileStream;

            Filename = filename;

            Context = context;

            Source = context.HttpContext.Request.RequestTraceIdentifier.ToString();

            Context.Download = this;
        }

        public void Dispose()
        {
            FileStream.Dispose(Source);
        }
    }
}
