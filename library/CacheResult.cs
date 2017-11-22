using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    class CacheResult
    {
        internal static byte[] Get(byte[] address)
        {
            string filename = Path.Combine(pParameters.json, Utils.ToBase64String(address));

            byte[] data = null;// DelayedWrite.Get(filename);

            if (data != null)
                return data;
            try
            {
                //Thread.Sleep(100);
                if (File.Exists(filename))
                {
                    return File.ReadAllBytes(filename);
                }
            }
            catch { }


            return null;
        }
    }
}
