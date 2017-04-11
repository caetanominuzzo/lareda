using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class p2pStream 
    {
        static Cache<p2pStream> streams = null;

        static Dictionary<string, long> source_position = new Dictionary<string, long>();

        public long Length { get { if (_stream != null && _stream.CanRead && (P2pFile == null || P2pFile.Status >= p2pFile.FileStatus.dataStructureComplete )) return _stream.Length; else return -1; } }

        public string Filename;

        Stream _stream;

        p2pFile P2pFile;


        p2pStream(string filename, string source, long initial_position)
        {
            Filename = filename;

            _stream = new FileStream(Filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            P2pFile = p2pFile.Queue.Get(Filename);

            lock(source_position)
            {
                if(!source_position.Keys.Contains(source + Filename))
                    source_position.Add(source + Filename, initial_position);
            }

        }

        public static p2pStream GetStream(string filename, string source, long initial_position)
        {
            if(streams == null)
            {
                streams = new Cache<p2pStream>(20 * 1000);

                streams.OnCacheExpired += Streams_OnCacheExpired;
            }

            CacheItem<p2pStream> stream = null;

            lock (streams)
                stream = streams.FirstOrDefault(x => x.CachedValue.Filename == filename);

            if (stream != null)
            {
                stream.Reset();
                
                if(!source_position.ContainsKey(source + filename))
                    source_position.Add(source + filename, initial_position);

                return stream.CachedValue;
            }

            return new p2pStream(filename, source, initial_position);
        }

        private static void Streams_OnCacheExpired(CacheItem<p2pStream> item)
        {
            item.CachedValue.Dispose();
        }

        public long GetPosition(string source)
        {
            lock (source_position)
            {
                if(source_position.Keys.Contains(source + Filename))
                    return source_position[source + Filename];
            }

            return 0;
        }

        public long Seek(long offset, SeekOrigin origin, string source)
        {
            var original_position = source_position[source + Filename];

            var result = _stream.Seek(offset, origin);

            lock(source_position)
                source_position[source + Filename] = result;

            return result;
        }

        public int Read(byte[] buffer, int offset, int count, string source)
        {
            var original_position = source_position[source + Filename];

            if (source_position[source + Filename] != _stream.Position)
            {
                lock (source_position)
                    source_position[source + Filename] = _stream.Seek(source_position[source + Filename], SeekOrigin.Begin);
            }

            lock (source_position)
                source_position[source + Filename] = _stream.Position;

            if (P2pFile != null && !P2pFile.CanReadFromLocalStream(source_position[source + Filename], count))
            {
                return -1;
            }

            var result = _stream.Read(buffer, offset, count);

            lock (source_position)
                source_position[source + Filename] = _stream.Position;

            return result;
        }

        public void Write(byte[] buffer, int offset, int count, string source)
        {
            if (source_position[source + Filename] != _stream.Position)
                lock (source_position)
                    source_position[source + Filename] = _stream.Seek(source_position[source + Filename], SeekOrigin.Begin);

            _stream.Write(buffer, offset, count);
        }

        internal void Dispose()
        {
            _stream.Dispose();
        }

        public void Dispose(string source)
        {
            lock (source_position)
            {
                source_position.Remove(source + Filename);

                if (!source_position.Any() && _stream != null)
                    _stream.Dispose();
            }
        }

    }
}
