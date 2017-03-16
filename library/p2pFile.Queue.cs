using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace library
{
    partial class p2pFile
    {
        internal static class Queue
        {

            static Cache<p2pFile> queue = null;

            internal static void Add(string base64Address, string filename, string specifFIle = null)
            {
                if (queue == null)
                {
                    queue = new Cache<p2pFile>(20 * 1000);

                    queue.OnCacheExpired += Queue_OnCacheExpired;
                }

                byte[] address = Utils.AddressFromBase64String(base64Address);

                Add(address, filename, specifFIle);
            }

            static void Add(byte[] address, string filename, string specifFIle = null)
            {
                Log.Write("add packet " + filename);

                //lock(queue)
                //{
                //    var item = queue.FirstOrDefault(x => Addresses.Equals(x.CachedValue.Address, address, true) &&
                //        x.CachedValue.Filename == filename);
                    
                //    if(item != null)
                //    {
                //        item.Reset();

                //        return;
                //    }
                //}

                p2pFile file = new p2pFile(address, filename, specifFIle);

                //root packet
                file.AddPacket(address, filename);

                lock (queue)
                    queue.Add(file);
            }

            private static void Queue_OnCacheExpired(CacheItem<p2pFile> item)
            {
                Log.Write("expire packet " + item.CachedValue.Filename);
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

                if(file.Success)
                    Client.DownloadComplete(file.Address, file.Filename, file.SpecifFilename);
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

                    Add(address, Encoding.Unicode.GetString(filename));
                }
            }
        }
    }
}