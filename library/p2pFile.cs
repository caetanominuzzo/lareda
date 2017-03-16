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

        internal bool Success = false;

        bool Cancel = false;

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

                        if (arrived.Count() == FilePackets.Count())
                            Success = true;

                        FilePackets.RemoveAll(x => x.Arrived);

                        FilePacketsArrived.AddRange(arrived);

                        dequeueOffset = 0;

                       
                    }

                    if(this.Filename.Contains("wIqQ9qIsc3es9hKs2bLxd"))
                    {

                    }

                    if (Utils.ToBase64String(this.Address).Contains("wIqQ9qIsc3es9hKs2bLxd"))
                        {

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

        internal Packet AddPacket(byte[] address, string filename = null)
        {
            Packet p = new Packet(this, address, filename);

            AddPacket(p);

            return p;
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
            Cancel = true;

            lock(FilePackets)
                FilePackets.Clear();
        }
    }

}
