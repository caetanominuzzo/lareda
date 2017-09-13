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
                        _queue = new Cache<p2pFile>(1002 * 1000);

                        _queue.OnCacheExpired += Queue_OnCacheExpired;
                    }

                    return _queue;
                }
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
                if(filename.EndsWith("cache/"))
                {

                }

                lock (queue)
                {
                    var item = queue.FirstOrDefault(x => Addresses.Equals(x.CachedValue.Address, address, true) &&
                        x.CachedValue.Filename == filename);

                    if (item != null)
                    {
                        item.CachedValue.AddContext(context);

                        item.Reset();


                        item.CachedValue.packetEvent.Set();

                        Log.Add(Log.LogTypes.Queue, Log.LogOperations.Add, new { RESET = 1, item.CachedValue});

                        return;
                    }
                }

                p2pFile file = new p2pFile(address, context, filename, specifFIle);

                Log.Add(Log.LogTypes.Queue, Log.LogOperations.Add, file);

                lock (queue)
                    queue.Add(file);

            }

            internal static void Reset(p2pFile file)
            {
                lock (queue)
                {
                    var item = queue.FirstOrDefault(x => x.CachedValue == file);

                    if (item != null)
                        item.Reset();
                }

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