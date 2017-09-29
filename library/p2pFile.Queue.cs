using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace library
{
    public partial class p2pFile
    {
        public static class Queue
        {
            static Cache<p2pFile> _queue = null;
            public static Cache<p2pFile> queue
            {
                get
                {
                    if (_queue == null)
                    {
                        _queue = new Cache<p2pFile>(20 * 1000);

                        _queue.OnCacheExpired += Queue_OnCacheExpired;
                    }

                    return _queue;
                }
            }

            internal static void Clear()
            {
                _queue = null;
            }

            internal static void Add(string base64Address, p2pContext context, string filename, string specifFIle = null)
            {
                byte[] address = Utils.AddressFromBase64String(base64Address);

                Add(address, context, filename, specifFIle);
            }

            internal static p2pFile Get(string filename)
            {
                CacheItem<p2pFile> result = null;

                lock (queue)
                    result = queue.FirstOrDefault(x => x.CachedValue.Filename == filename);

                if (result == null)
                    return null;

                return result.CachedValue;
            }


            public static IEnumerable<p2pFile> All()
            {
                return p2pFile.Queue.queue.Items();
            }

            static void Add(byte[] address, p2pContext context, string filename, string specifFIle = null)
            {
                CacheItem<p2pFile> cacheItem = null;

                lock (queue)
                {
                    cacheItem = queue.FirstOrDefault(x => Addresses.Equals(x.CachedValue.Address, address, true) &&
                        x.CachedValue.Filename == filename);
                }
                if (cacheItem != null)
                {
                    cacheItem.CachedValue.AddContext(context);

                    cacheItem.Reset();

                    cacheItem.CachedValue.packetEvent.Set();

                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.Add | Log.LogOperations.File, new { RESET = 1, cacheItem.CachedValue });

                    return;
                }

                p2pFile file = new p2pFile(address, context, filename, specifFIle);
                 
                Log.Add(Log.LogTypes.Queue, Log.LogOperations.Add | Log.LogOperations.File, file);

                lock (queue)
                    queue.Add(file);

            }

            internal static void Reset(p2pFile file)
            {
                CacheItem<p2pFile> cacheItem = null;

                lock (queue)
                    cacheItem = queue.FirstOrDefault(x => x.CachedValue == file);

                if (cacheItem != null)
                    cacheItem.Reset();

            }

            private static void Queue_OnCacheExpired(CacheItem<p2pFile> item)
            {
                Log.Add(Log.LogTypes.Queue, Log.LogOperations.Expire, item.CachedValue);

                item.CachedValue.Dispose();
            }

            internal static void QueueComplete(p2pFile file)
            {
                CacheItem<p2pFile> cacheItem = null;

                lock (queue)
                {
                    cacheItem = queue.FirstOrDefault(x => x.CachedValue.Address != null && Addresses.Equals(x.CachedValue.Address, file.Address, true));

                    if (cacheItem != null)
                    {
                        queue.Remove(cacheItem);
                    }
                }

                if (file.Success)
                    Client.DownloadComplete(file.Address, file.Filename, file.SpecifFilename, file.Arrives, file.Cursors);
            }

            private static void Save()
            {
                lock (queue)
                {
                    Client.Stats.belowMaxReceivedEvent.Set();

                    queue.ForEach(x => x.CachedValue.stoppedEvent.WaitOne());

                    List<byte> buffer = new List<byte>();

                    foreach (var item in queue)
                    {
                        var file = item.CachedValue;

                        buffer.AddRange(BitConverter.GetBytes(file.Address.Length));
                        buffer.AddRange(file.Address);

                        byte[] filename = Encoding.Unicode.GetBytes(file.Filename);

                        buffer.AddRange(BitConverter.GetBytes(filename.Length));
                        buffer.AddRange(filename);
                    }

                    File.WriteAllBytes(pParameters.fileQueuePath, buffer.ToArray());
                }
            }

            internal static void Load()
            {
                if (!File.Exists(pParameters.fileQueuePath))
                    return;

                byte[] buffer = File.ReadAllBytes(pParameters.fileQueuePath);

                int offset = 0;

                int count = buffer.Length;

                while (offset < count)
                {
                    byte[] address = Utils.ReadBytes(buffer, offset);

                    offset += 4 + address.Length;

                    byte[] filename = Utils.ReadBytes(buffer, offset);

                    offset += 4 + filename.Length;

                    Add(address, null, Encoding.Unicode.GetString(filename));
                }
            }
        }
    }
}