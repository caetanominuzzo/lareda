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
            static List<p2pFile> queue = new List<p2pFile>();

            internal static void Add(string base64Address, string filename, string specifFIle = null)
            {
                byte[] address = Utils.AddressFromBase64String(base64Address);

                Add(address, filename, specifFIle);
            }

            static void Add(byte[] address, string filename, string specifFIle = null)
            {
                p2pFile file = new p2pFile(address, filename, specifFIle);

                //root packet
                file.AddPacket(address, filename);

                lock (queue)
                    queue.Add(file);
            }

            internal static void QueueComplete(p2pFile file)
            {
                lock (queue)
                    queue.Remove(file);

                Client.DownloadComplete(file.Address, file.Filename, file.SpecifFilename);
            }

            private static void Save()
            {
                lock (queue)
                {
                    Client.Stats.belowMaxReceivedEvent.Set();

                    queue.ForEach(x => x.stoppedEvent.WaitOne());

                    List<byte> buffer = new List<byte>();

                    foreach (p2pFile file in queue)
                    {
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