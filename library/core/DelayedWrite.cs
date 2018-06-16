using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace library
{
    public static class DelayedWrite
    {
        public static byte[] Get(string filename)
        {
            DelayedWriteItem item = null;

            lock (queue)
                item = queue.FirstOrDefault(x => x.Filename == filename);

            if (null != item)
                return item.Data.Skip(item.ReadOffset).ToArray();

            return null;
        }
        /*
        public static byte[] ReadFile(string filename, int bytesOffset, int bytesCount)
        {
            var packetOffsetStart = ((int)bytesOffset / pParameters.packetSize) + 1;

            var packetOffsetEnd = ((int)(bytesOffset + bytesCount) / pParameters.packetSize) + 1;

            var result = new List<byte>();

            lock(queue)
            {
                queue.Where(x => x.Filename == filename && x.Offset >= packetOffsetStart && x.Offset <= packetOffsetEnd);
            }
        }
        */
        #region Thread Refresh

        static List<DelayedWriteItem> queue = new List<DelayedWriteItem>();

        static ManualResetEvent itemsOnQueueEvent = new ManualResetEvent(false);

        internal static void Start()
        {
            Thread thread = new Thread(Refresh);

            thread.Name = "DelayedWrite";

            thread.Start();
        }

        internal static void Stop()
        {
            itemsOnQueueEvent.Set();
        }

        static void Refresh()
        {
            IEnumerable<DelayedWriteItem> items = null;

            DelayedWriteItem item = null;

            while (!Client.Stop)
            {
                Log.Add(Log.LogTypes.Journaling, Log.LogOperations.Refresh, "3DnFpsP2x");

                lock (queue)
                    item = queue.Where(x => x.writeToCacheDir).ToList().OrderBy(x => -x.Offset).FirstOrDefault();

                if (null != item)
                    Write(item);


                lock (queue)
                    items = queue.Where(x => !x.writeToCacheDir).ToList();

                foreach (var i in items)
                    Write(i);

                lock (queue)
                    if (!queue.Any())
                        itemsOnQueueEvent.Reset();

                itemsOnQueueEvent.WaitOne();
            }
        }

        static void Write(DelayedWriteItem item)
        {
            Log.Add(Log.LogTypes.Journaling, Log.LogOperations.Write, item);

            try
            {
                if (!item.writeToCacheDir)
                {
                    File.OpenWrite(item.Filename).Write(item.Data, item.ReadOffset, item.Data.Length - item.ReadOffset);

                    //File.WriteAllBytes(item.Filename, item.Data);

                    Remove(item); 
                }
                else
                {
                    if (!Directory.Exists(Path.GetDirectoryName(item.Filename)))
                        Directory.CreateDirectory(Path.GetDirectoryName(item.Filename));

                    p2pStream stream = null;

                    var context = new p2pContext(null, true);

                    try
                    {
                        stream = p2pStreamManager.GetStream(item.Filename, context, 0);

                        DelayedWriteItem[] same_file;

                        lock (queue)
                            same_file = queue.Where(x => x.Filename == item.Filename).OrderBy(x => x.Offset).ToArray();

                        while (same_file.Length > 0)
                        {
                            Log.Add(Log.LogTypes.Journaling, Log.LogOperations.Write, same_file);

                            List<DelayedWriteItem> toRemove = new List<DelayedWriteItem>();

                            ///////////////
                            var last = same_file.Last();

                            if(!last.Filename.Contains(pParameters.webCache))
                                stream.Write(last.Data, last.ReadOffset, last.Offset * pParameters.packetSize, last.Data.Length - last.ReadOffset, context);

                            toRemove.Add(last);
                            ///////////////

                            foreach (DelayedWriteItem writeItem in same_file)
                            {
                                if (writeItem == last)
                                    break;

                                if (!writeItem.Filename.Contains(pParameters.webCache))
                                    stream.Write(writeItem.Data, writeItem.ReadOffset, writeItem.Offset * pParameters.packetSize, writeItem.Data.Length - writeItem.ReadOffset, context);

                                toRemove.Add(writeItem);
                            }

                            lock (queue)
                            {
                                Remove(list: toRemove);

                                same_file =
                                    queue.Where(x => x.Filename == item.Filename).OrderBy(x => x.Offset).ToArray();
                            }
                        }
                    }
                    finally
                    {
                        stream.Dispose(context);
                    }
                }
            }
            catch (Exception e)
            {

            }
        }

        static void Remove(DelayedWriteItem item = null, List<DelayedWriteItem> list = null)
        {
            var count = 0;

            lock (queue)
            {
                if (item != null)
                    queue.Remove(item);
                else if (list != null)
                    queue.RemoveAll(x => list.Any(y => y == x));

                count = queue.Count();
            }
        }

        #endregion

        internal static void Add(string filename, byte[] data, int offset = 1, int readOffset = 0)
        {
            var count = 0;

            var item = new DelayedWriteItem { Filename = filename, Data = data, Offset = offset, ReadOffset = readOffset };

            Log.Add(Log.LogTypes.Journaling, Log.LogOperations.Add, item);

            lock (queue)
            {
                queue.Add(item);

                count = queue.Count();
            }

            if (count == 1) 
                itemsOnQueueEvent.Set();
            //else if (count >= pParameters.MaxDelayedWriteQueue)
            //{
            //    freeQueueEvent.Reset();

            //    freeQueueEvent.WaitOne();
            //}
        }

        class DelayedWriteItem
        {
            internal bool writeToCacheDir;

            string filename;

            public string Filename
            {
                get
                {
                    return filename;
                }
                set
                {
                    filename = value;

                    writeToCacheDir = (Path.GetDirectoryName(filename) != pParameters.localPacketsDir && Path.GetDirectoryName(filename) != pParameters.json);
                }
            }

            public int Offset = 1;

            public int ReadOffset = 0;

            internal byte[] Data;
        }
    }
}
