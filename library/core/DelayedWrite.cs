﻿using System;
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
            lock (queue)
            {
                DelayedWriteItem item = queue.FirstOrDefault(x => x.Filename == filename);

                if (item != null)
                    return item.Data;
            }

            return null;
        }

        #region Thread Refresh

        static List<DelayedWriteItem> queue = new List<DelayedWriteItem>();

        static ManualResetEvent itemsOnQueueEvent = new ManualResetEvent(false);

        static ManualResetEvent freeQueueEvent = new ManualResetEvent(true);

        internal static void Start()
        {
            Thread thread = new Thread(Refresh);

            thread.Start();
        }

        internal static void Stop()
        {
            itemsOnQueueEvent.Set();

            freeQueueEvent.Set();
        }

        static void Refresh()
        {
            while (!Client.Stop)
            {
                lock (queue)
                {
                    var items = queue.Where(x => !x.writeToPacketsDir).ToList();

                    foreach (var item in items)
                        Write(item);

                    items = queue.Where(x => x.writeToPacketsDir).ToList();

                    foreach (var item in items)
                        Write(item);

                    if (!queue.Any())
                        itemsOnQueueEvent.Reset();
                }

                itemsOnQueueEvent.WaitOne();
            }
        }

        static void Write(DelayedWriteItem item)
        {
            try
            {
                if (item.writeToPacketsDir)
                {
                    File.WriteAllBytes(item.Filename, item.Data);

                    Remove(item);
                }
                else
                {
                    if (!Directory.Exists(Path.GetDirectoryName(item.Filename)))
                        Directory.CreateDirectory(Path.GetDirectoryName(item.Filename));

                    using (Stream stream = new FileStream(item.Filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        DelayedWriteItem[] same_file;

                        lock (queue)
                            same_file = queue.Where(x => x.Filename == item.Filename).OrderBy(x => x.Offset).ToArray();

                        while (same_file.Length > 0)
                        {
                            List<DelayedWriteItem> toRemove = new List<DelayedWriteItem>();

                            foreach (DelayedWriteItem writeItem in same_file)
                            {
                                stream.Seek(writeItem.Offset * pParameters.packetSize, 0);

                                stream.Write(writeItem.Data, 0, writeItem.Data.Length);

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

            if (count < pParameters.MaxDelayedWriteQueue)
                freeQueueEvent.Set();
        }

        #endregion

        internal static void Add(string filename, byte[] data, int offset = 1)
        {
            var count = 0;

            lock (queue)
            {
                queue.Add(new DelayedWriteItem { Filename = filename, Data = data, Offset = offset });

                count = queue.Count();
            }

            if (count == 1)
                itemsOnQueueEvent.Set();
            else if (count >= pParameters.MaxDelayedWriteQueue)
            {
                freeQueueEvent.Reset();

                freeQueueEvent.WaitOne();
            }


        }

        class DelayedWriteItem
        {
            internal bool writeToPacketsDir;

            string filename;

            internal string Filename
            {
                get
                {
                    return filename;
                }
                set
                {
                    filename = value;

                    writeToPacketsDir = Path.GetDirectoryName(filename) == pParameters.localPacketsDir;
                }
            }

            internal int Offset = 1;

            internal byte[] Data;
        }
    }
}
