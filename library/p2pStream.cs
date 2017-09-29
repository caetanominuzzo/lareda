using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static library.p2pFile;

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
                if (P2pFile != null)
                    return P2pFile.Length;

                return length;
            }
        }

        public string Filename;

        p2pFile P2pFile;

        public void InitialLoadDone()
        {
            if (null != P2pFile)
                P2pFile.gotLastsPackets.Set();
        }

        internal p2pStream(string filename, p2pContext context, long initial_position)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Open, new { context, Filename, initial_position });

            Filename = filename;

            var fileExists = File.Exists(Filename);

            if (fileExists)
            {
                var fi = new FileInfo(Filename);

                length = fi.Length;

                try
                {
                    // _stream = MemoryMappedFile.CreateFromFile(Filename, FileMode.Open, RemoveInvalidFilePathCharacters(Filename), length);
                }
                catch (Exception e)
                {

                }
            }

            if (!fileExists)
                P2pFile = p2pFile.Queue.Get(Filename);

            lock (source_position)
                if (!source_position.ContainsKey(context))
                    source_position.Add(context, initial_position);
        }

        static string RemoveInvalidFilePathCharacters(string filename, string replaceChar = "_")
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(filename, replaceChar);
        }

        public long GetPosition(p2pContext context)
        {
            lock (source_position)
                return source_position[context];
        }

        public int Read(byte[] buffer, int offset, int count, p2pContext context, out Packet[] packets)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Read, new { context, Filename, offset, count });

            packets = null;

            if (P2pFile != null)
                return P2pFile.TryReadFromPackets(buffer, offset, count, out packets);

            if (offset == length)
                return 0;

            if (offset + count > length)
                count = (int)length - offset;

            try
            {

                using (var mmf = MemoryMappedFile.CreateFromFile(Filename, FileMode.Open, RemoveInvalidFilePathCharacters(Filename), length))
                using (var accessor = mmf.CreateViewAccessor(offset, count))
                    return accessor.ReadArray(0, buffer, 0, count);
            }
            catch (Exception e)
            {
                Log.Add(Log.LogTypes.File, Log.LogOperations.Exception, new { e, context, Filename, offset, count });

            }
            return -1;
        }

        public void Write(byte[] buffer, int sourceOffset, int offset, int count, p2pContext context)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Write, new { context, Filename, sourceOffset, offset, count, length, new_length = offset + count });


            if(offset + count > length)
                length = offset + count;

            try
            {
                using (var mmf = MemoryMappedFile.CreateFromFile(Filename, FileMode.OpenOrCreate, RemoveInvalidFilePathCharacters(Filename), length))
                using (var accessor = mmf.CreateViewAccessor(offset, count))
                    accessor.WriteArray(0, buffer, sourceOffset, count);
            }
            catch (Exception e)
            {
                Log.Add(Log.LogTypes.File, Log.LogOperations.Exception, new { e, context, Filename, sourceOffset, offset, count });
            }
        }

        internal void Dispose()
        {
        }

        public void Dispose(p2pContext context)
        {
            Log.Add(Log.LogTypes.File, Log.LogOperations.Dispose, new { context, Filename });

            if (Filename.Contains("3DnFpsP2xPTUm"))
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
