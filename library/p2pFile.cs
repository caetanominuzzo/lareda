using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace library
{
    internal partial class p2pFile : IDisposable
    {
        internal byte[] Address;

        internal string Filename;

        internal string SpecifFilename = string.Empty;

        List<Packet> FilePackets = new List<Packet>();

        List<Packet> FilePacketsArrived = new List<Packet>();

        internal double ReturnRatio = 1;

        internal ManualResetEvent stoppedEvent = new ManualResetEvent(false);

        int dequeueOffset;

        internal p2pFile(byte[] address, string filename = null, string specifFile = null)
        {
            Address = address;

            Filename = filename;

            if(!string.IsNullOrEmpty(specifFile))
                SpecifFilename = specifFile;

            Thread thread = new Thread(Refresh);

            thread.Start();
        }

        void Refresh()
        {
            while (!Client.Stop)
            {
                Packet packet;

                lock (FilePackets)
                {
                    if (dequeueOffset == FilePackets.Count()) //Math.Min(Packets.Count(), 1000))
                    {
                        IEnumerable<Packet> arrived = FilePackets.Where(x => x.Arrived);

                        FilePackets.RemoveAll(x => x.Arrived);

                        FilePacketsArrived.AddRange(arrived);

                        dequeueOffset = 0;

                       
                    }

                    if (!FilePackets.Any())
                    {
                        Queue.QueueComplete(this);
                        break;
                        
                    }
                    else
                    {

                        packet = FilePackets[dequeueOffset++];

                        packet.Get();
                    }
                }



                        
            }

            stoppedEvent.Set();
        }

        internal void AddPacket(byte[] address, string filename = null)
        {
            Packet p = new Packet(this, address, filename);

            AddPacket(p);
        }

        void AddPacket(Packet packet)
        {
            lock (FilePackets)
                FilePackets.Add(packet);
        }

        bool MayHaveLocalData()
        {
            lock (FilePackets)
                return FilePackets.Any(x => x.MayHaveLocalData);
        }

        public void Dispose()
        {
            stoppedEvent.Close();
        }
    }
}
