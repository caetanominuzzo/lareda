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
                    queue = new Cache<p2pFile>(10 * 1000);

                    queue.OnCacheExpired += Queue_OnCacheExpired;
                }

                byte[] address = Utils.AddressFromBase64String(base64Address);

                Add(address, filename, specifFIle);
            }


            public static IEnumerable<p2pFile> All()
            {
                return p2pFile.Queue.queue.Items();
            }

            static void Add(byte[] address, string filename, string specifFIle = null)
            {
                

                lock (queue)
                {
                    var item = queue.FirstOrDefault(x => Addresses.Equals(x.CachedValue.Address, address, true) &&
                        x.CachedValue.Filename == filename);

                    if (item != null)
                    {
                        item.Reset();

                        return;
                    }
                }

                p2pFile file = new p2pFile(address, filename, specifFIle);

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
                Log.Write("expire file \t[" + Utils.ToSimpleAddress(item.CachedValue.Address) + "]\t " +  item.CachedValue.Filename, Log.LogTypes.queueExpireFile);

                item.CachedValue.Dispose();
            }

            internal static void Print()
            {
                Log.Write("### DOWNLOAD QUEUE ###", Log.LogTypes.Ever);

                foreach(var f in queue)
                {
                    Log.Write("File:\t[" + Utils.ToSimpleAddress(f.CachedValue.Address) + "]\t[" + f.CachedValue.Filename, Log.LogTypes.Ever, 1);

                    Log.Write("Packets queue:\t" + f.CachedValue.FilePackets.Count(), Log.LogTypes.Ever, 2);

                    Log.Write("Packets arrived:\t" + f.CachedValue.FilePacketsArrived.Count(), Log.LogTypes.Ever, 2);

                    var count = 0;

                    foreach(var t in f.CachedValue.FilePackets)
                    {
                        Log.Write("Packet:\t[" + Utils.ToSimpleAddress(t.Address) , Log.LogTypes.Ever, 2);

                        if (count++ > 10)
                        {
                            Log.Write("...", Log.LogTypes.Ever, 3);

                            Log.Write("Packet:\t[" + Utils.ToSimpleAddress(f.CachedValue.FilePackets.Last().Address), Log.LogTypes.Ever, 3);

                            break;
                        }
                    }
                }
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