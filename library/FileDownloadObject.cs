using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace library
{
    public class FileDownloadObject : IDisposable
    {
        public ManualResetEvent packetArrivedEvent = new ManualResetEvent(false);

        public byte[] Address;

        [JsonIgnore]
        public p2pStream FileStream;

        [JsonIgnore]
        public p2pContext Context;

        public string Filename;

        public FileDownloadObject(byte[] address, p2pContext context, string filename, p2pStream fileStream = null)
        {
            Address = address;

            FileStream = fileStream;

            Filename = filename;

            Context = context;

            Context.Download = this;
        }

        public void Dispose()
        {
            Log.Add(Log.LogTypes.Stream, Log.LogOperations.Dispose, this);

            //if(null != Context && null != Context.OutputStream)
            //{
            //    Context.OutputStream.Close();

            //    Context.HttpContext.Response.Close();
            //}

            if(FileStream != null)
                FileStream.Dispose();
        }
    }
}
