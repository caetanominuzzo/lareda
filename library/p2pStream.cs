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
        internal Dictionary<p2pContext, long> source_position = new Dictionary<p2pContext, long>();

        long length = -1;

        public long Length
        {
            get
            {
                return length;
            }
        }

        public string Filename;

        Stream _stream;

        p2pFile P2pFile;

        internal p2pStream(string filename, p2pContext context, long initial_position)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Open, new { context, Filename, initial_position });

            Filename = filename;

            var fileExists = File.Exists(Filename);

            _stream = new FileStream(Filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            length = _stream.Length;

            if(!fileExists)
                P2pFile = p2pFile.Queue.Get(Filename);

            lock (source_position)
                if (!source_position.ContainsKey(context))
                    source_position.Add(context, initial_position);
        }

        public long GetPosition(p2pContext context)
        {
            lock (source_position)
                return source_position[context];
        }

        public long Seek(long offset, SeekOrigin origin, p2pContext context)
        {
            return source_position[context] = _stream.Seek(offset, origin);
        }

        public int Read(byte[] buffer, int offset, int count, p2pContext context)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Read, new { context, Filename, offset, count });

            if (source_position[context] != _stream.Position)
                _stream.Seek(source_position[context], SeekOrigin.Begin);

            if (P2pFile != null && !P2pFile.CanReadFromLocalStream(source_position[context], 1))
                return -1;

            var result = _stream.Read(buffer, offset, count);

            source_position[context] = _stream.Position;

            return result;
        }

        public void Write(byte[] buffer, int offset, int count, p2pContext context)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Write, new { context, Filename, offset, count });

            if (source_position[context] != _stream.Position)
                source_position[context] = _stream.Seek(source_position[context], SeekOrigin.Begin);

            _stream.Write(buffer, offset, count);

            if (_stream.Length > length)
                length = _stream.Length;
        }

        internal void Dispose()
        {
            _stream.Dispose();
        }

        public void Dispose(p2pContext context)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Dispose, new { context, Filename });

            if(Filename.Contains("3DnFpsP2xPTUm"))
            {

            }

            lock (source_position)
                source_position.Remove(context);

            //if (!source_position.Any() && _stream != null)
            //{
            //    _stream.Dispose();

            //    Log.Add(Log.LogTypes.File | Log.LogTypes.Dispose, new { Filename });
            //}
        }
    }

}
