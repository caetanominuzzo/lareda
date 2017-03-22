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

        internal ManualResetEvent newPacketEvent = new ManualResetEvent(false);

        int dequeueOffset;

        internal bool Success = false;

        internal bool Cancel = false;

        Packet Root;

        enum FileStatus
        {
            addressStructureIncomplete = 0,
            addressStructureComplete = 1,
            dataStructureComplete = 2
        }

        FileStatus status = FileStatus.addressStructureIncomplete;

        FileStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                if (value == FileStatus.dataStructureComplete &&
                    status != FileStatus.dataStructureComplete)
                {
                    Log.Write("file ready:\t[" + Utils.ToSimpleAddress(Address), Log.LogTypes.queue);

                    //OnFileDownload
                }

                status = value;
            }
        }

        internal p2pFile(byte[] address, string filename = null, string specifFile = null)
        {
            Address = address;

            Filename = filename;
            
            Log.Write("add file \t[" + Utils.ToSimpleAddress(Address) + "]\t " + filename, Log.LogTypes.queueAddFile);

            if (!string.IsNullOrEmpty(specifFile))
                SpecifFilename = specifFile;

            Root = AddPacket(address, null, filename);

            Thread thread = new Thread(Refresh3);

            thread.Start();
        }

        enum RefreshSteps
        {
            Initial = 0,
            Final = 1,
            Sequencial = 2
        }

        RefreshSteps step = RefreshSteps.Initial;


        void Refresh2()
        {

        }

        void Refresh()
        {
            while (!Client.Stop && !Cancel)
            {
                Packet packet = null;

                var end_of_packets = false;

                lock (FilePackets)
                    end_of_packets = dequeueOffset == FilePackets.Count();

                Log.Write("Queue Item Refresh: [" + Utils.ToSimpleAddress(Address) + "]\tPackets: " + FilePackets.Count() + "\tOffset: " + dequeueOffset + "\tArrived: " + FilePacketsArrived.Count(), Log.LogTypes.queue);

                if (end_of_packets)
                {

                    newPacketEvent.Reset();

                    Log.Write("end of packets:\t[" + Utils.ToSimpleAddress(Address), Log.LogTypes.queueEndOfPackets);

                    if (newPacketEvent.WaitOne(pParameters.restart_requesting_packets_from_coda_timeout))
                    {
                        end_of_packets = false;

                        Log.Write("new packets arrived t[" + Utils.ToSimpleAddress(Address), Log.LogTypes.queueAddPacket);
                    }
                    else
                    {

                        Log.Write("timeout: no new packets arrived t[" + Utils.ToSimpleAddress(Address), Log.LogTypes.queueLastPacketTimeout);

                        lock (FilePackets)
                        {
                            IEnumerable<Packet> arrived = FilePackets.Where(x => x.Arrived);

                            if (arrived.Count() == FilePackets.Count())
                                Success = true;

                            FilePackets.RemoveAll(x => x.Arrived);

                            FilePacketsArrived.AddRange(arrived);
                        }

                        dequeueOffset = 0;

                        step = RefreshSteps.Initial;

                        if (!Success)
                            break;
                    }
                }

                lock (FilePackets)
                {
                    if (Root != null && !FilePackets.Any())
                    {
                        Queue.QueueComplete(this);

                        Log.Write("file end:\t[" + Utils.ToSimpleAddress(Address) + "]\t " + Filename, Log.LogTypes.queueFileComplete);

                        break;

                    }

                    switch (step)
                    {
                        case RefreshSteps.Sequencial:
                        case RefreshSteps.Initial:

                            packet = FilePackets[dequeueOffset++];

                            if (step == RefreshSteps.Initial)
                                step = RefreshSteps.Final;

                            break;

                        case RefreshSteps.Final:

                            packet = FilePackets[FilePackets.Count() - 1];

                            step = RefreshSteps.Sequencial;

                            break;

                    }

                    packet.Get();

                    p2pFile.Queue.Reset(this);

                }
            }

            stoppedEvent.Set();
        }

        bool Ended
        {
            get
            {
                lock (FilePackets)
                    return Cancel || Success || !FilePackets.Any() || FilePackets.All(x => x.Arrived);
            }
        }

        void Refresh3()
        {
            while (!Client.Stop && !Ended)
            {
                Packet packet = null;

                var end_of_packets = false;

                lock (FilePackets)
                    end_of_packets = dequeueOffset == FilePackets.Count();

                Log.Write("Queue Item Refresh: [" + Utils.ToSimpleAddress(Address) + "]\tPackets: " + FilePackets.Count() + "\tOffset: " + dequeueOffset + "\tArrived: " + FilePacketsArrived.Count(), Log.LogTypes.queue);

                lock (FilePackets)
                    while (dequeueOffset == FilePackets.Count())
                    {
                        newPacketEvent.Reset();

                        if (newPacketEvent.WaitOne(pParameters.restart_requesting_packets_from_coda_timeout))
                        {
                            IEnumerable<Packet> arrived = FilePackets.Where(x => x.Arrived);

                            FilePackets.RemoveAll(x => x.Arrived);

                            FilePacketsArrived.AddRange(arrived);

                            if (!FilePackets.Any())
                            {
                                Success = true;

                                break;
                            }
                        }
                        else
                        {
                            Log.Write("timeout: no new packets arrived t[" + Utils.ToSimpleAddress(Address), Log.LogTypes.queueLastPacketTimeout);

                            dequeueOffset = 0;

                            step = RefreshSteps.Initial;

                            Cancel = true;

                            break;
                        }
                    }

                if (Success)
                {
                    Queue.QueueComplete(this);

                    Log.Write("file end:\t[" + Utils.ToSimpleAddress(Address) + "]\t " + Filename, Log.LogTypes.queueFileComplete);

                    break;

                }
                else if(Cancel)
                {
                    break;
                }


                switch (step)
                {
                    case RefreshSteps.Sequencial:
                    case RefreshSteps.Initial:

                        packet = FilePackets[dequeueOffset++];

                        if (step == RefreshSteps.Initial)
                            step = RefreshSteps.Final;

                        break;

                    case RefreshSteps.Final:

                        packet = FilePackets[FilePackets.Count() - 1];

                        step = RefreshSteps.Sequencial;

                        break;

                }

                packet.Get();

                p2pFile.Queue.Reset(this);

            }

            stoppedEvent.Set();
        }

        internal IEnumerable<Packet> AddPacketRange(IEnumerable<byte[]> addresses, p2pFile.Packet parent, string filename = null)
        {
            foreach (var address in addresses)
                yield return AddPacket(address, parent, filename);
        }

        internal Packet AddPacket(byte[] address, p2pFile.Packet parent, string filename = null)
        {
            Packet p = new Packet(this, parent, address, filename);

            AddPacket(p);

            return p;
        }

        void AddPacket(Packet packet)
        {
            lock (FilePackets)
                FilePackets.Add(packet);

            Log.Write("add packets: [" + Utils.ToSimpleAddress(this.Address) + "] [" + Utils.ToSimpleAddress(packet.Address), Log.LogTypes.queueAddPacket);

            newPacketEvent.Set();
        }

        bool MayHaveLocalData()
        {
            lock (FilePackets)
                return FilePackets.Any(x => x.MayHaveLocalData);
        }

        public void Dispose()
        {
            Log.Write("file disposed: [" + Utils.ToSimpleAddress(this.Address), Log.LogTypes.queueFileDisposed);

            this.Cancel = true;

            lock (FilePackets)
                FilePackets.Clear();
        }
    }

}
