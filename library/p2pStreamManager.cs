using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class p2pStreamManager
    {
        static Cache<p2pStream> streams = null;

        public static p2pStream GetStream(string filename, p2pContext context, long initial_position = -1)
        {
            if (streams == null)
            {
                streams = new Cache<p2pStream>(20 * 1000 * 1000);

                streams.OnCacheExpired += Streams_OnCacheExpired;
            }

            CacheItem<p2pStream> stream = null;

            lock (streams)
                stream = streams.FirstOrDefault(x => x.CachedValue.Filename == filename);

            if (stream != null)
            {
                stream.Reset();

                if (initial_position != -1)
                    stream.CachedValue.source_position[context] = initial_position;

                return stream.CachedValue;
            }

            var result = new p2pStream(filename, context, initial_position);

            streams.Add(result);

            return result;
        }

        private static void Streams_OnCacheExpired(CacheItem<p2pStream> item)
        {
            item.CachedValue.Dispose();
        }
    }
}
