using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace library
{
    internal partial class p2pFile : IDisposable
    {
        internal byte[] Address;

        public string Filename;

        internal string SpecifFilename = string.Empty;

        SortedList<int, Packet> FilePackets = new SortedList<int, Packet>();

        List<Packet> FilePacketsArrived = new List<Packet>();

        internal double ReturnRatio = 1;

        internal ManualResetEvent stoppedEvent = new ManualResetEvent(false);

        internal ManualResetEvent newPacketEvent = new ManualResetEvent(false);

        int dequeueOffset;

        public bool Success = false;

        public bool Cancel = false;

        Packet Root;

        Packet First;

        Packet SecondLast;

        Packet Last;

        List<HttpListenerContext> Context = new List<HttpListenerContext>();

        public IEnumerable<Guid> RequestTraceIdentifier
        {
            get
            {
                return Context.Select(x => x.Request.RequestTraceIdentifier);
            }
        }

        enum FileStatus
        {
            addressStructureIncomplete = 0,
            addressStructureComplete = 1,
            dataStructureComplete = 2,
            dataComplete = 3

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
                if ((value == FileStatus.dataStructureComplete || value == FileStatus.dataComplete) &&
                    (status != FileStatus.dataStructureComplete && status != FileStatus.dataComplete))
                {
                    Log.Add(Log.LogTypes.queueDataStructureComplete, this.Filename);

                    Client.DownloadComplete(Address, Filename, SpecifFilename);
                }

                status = value;
            }
        }

        internal void AddContext(HttpListenerContext context)
        {
            lock (Context)
                Context.Add(context);
        }

        internal p2pFile(byte[] address, HttpListenerContext context, string filename = null, string specifFile = null)
        {
            Address = address;

            Filename = filename;

            lock (Context)
                Context.Add(context);

            Log.Add(Log.LogTypes.queueAddFile, this);

            if (!string.IsNullOrEmpty(specifFile))
                SpecifFilename = specifFile;

            Root = AddPacket(address, null, 0, filename);

            Thread thread = new Thread(Refresh3);

            thread.Start();
        }

        enum RefreshSteps
        {
            Initial = 0,
            FinalMinusOne = 1,
            Final = 2,
            Sequencial = 3
        }

        RefreshSteps step = RefreshSteps.Sequencial;
        bool Ended
        {
            get
            {
                lock (FilePackets)
                    return Cancel || Success || !FilePackets.Any() || FilePackets.All(x => x.Value.Arrived);
            }
        }

        bool AnyContext()
        {
            lock (Context)
            {
                for (var i = Context.Count() - 1; i >= 0; i--)
                {
                    var c = Context[i];

                    try
                    {
                        lock (c)
                            if (!c.Response.OutputStream.CanWrite)
                                Context.RemoveAt(i);
                    }
                    catch
                    {
                        Context.RemoveAt(i);
                    }
                }
            }

            return Context.Any();
        }

        void Refresh3()
        {
            while (!Client.Stop && !Ended)
            {
                Log.Add(Log.LogTypes.Ever, new { First = FilePackets.First().Key, Last = FilePackets.Last().Key });


                if (!AnyContext())
                {
                    Cancel = true;

                    break;
                }

                Packet packet = null;

                var end_of_packets = false;

                lock (FilePackets)
                    end_of_packets = dequeueOffset == FilePackets.Count();

                Log.Add(Log.LogTypes.queueRefresh, this);

                var resetDequeueOffset = false;

                lock (FilePackets)
                    resetDequeueOffset = dequeueOffset == FilePackets.Count() || !FilePackets.Any();


                if (resetDequeueOffset)
                {
                    newPacketEvent.Reset();

                    if (newPacketEvent.WaitOne(pParameters.restart_requesting_packets_from_coda_timeout))
                    {
                        lock (FilePackets)
                        {
                            for (var i = FilePackets.Count() - 1; i >= 0; i--)
                                if (FilePackets.ElementAt(i).Value.Arrived)
                                    FilePackets.RemoveAt(i);

                            if (!FilePackets.Any())
                            {
                                Success = true;

                                break;
                            }
                            else
                                dequeueOffset = 0;
                        }
                    }
                    else
                    {
                        dequeueOffset = 0;

                        step = RefreshSteps.Initial;

                        //Cancel = true;

                        //break;
                    }
                }


                switch (step)
                {
                    case RefreshSteps.Sequencial:
                    case RefreshSteps.Initial:

                        lock (FilePackets)
                            packet = FilePackets.ElementAt(dequeueOffset).Value;

                        dequeueOffset = Math.Min(FilePackets.Count(), dequeueOffset + 1);

                        if (step == RefreshSteps.Initial)
                            step = RefreshSteps.FinalMinusOne;  

                        break;

                    case RefreshSteps.FinalMinusOne:


                        if (FilePackets.Count() >= 2)
                        {
                            packet = FilePackets.ElementAt(FilePackets.Count() - 2).Value;

                            step = RefreshSteps.Final;
                        }
                        else
                        {
                            packet = FilePackets.ElementAt(FilePackets.Count() - 1).Value;

                            step = RefreshSteps.Sequencial;
                        }



                        break;

                    case RefreshSteps.Final:

                        packet = FilePackets.ElementAt(FilePackets.Count() - 1).Value;

                        step = RefreshSteps.Sequencial;

                        break;

                }

                packet.Get();

                p2pFile.Queue.Reset(this);

            }

            if (Success)
            {
                Queue.QueueComplete(this);

                Log.Add(Log.LogTypes.queueFileComplete, this);
            }

            stoppedEvent.Set();
        }


        internal Packet AddPacket(byte[] address, p2pFile.Packet parent, int offset, string filename = null)
        {
            Packet p = new Packet(this, parent, offset, address, filename);

            AddPacket(p);

            return p;
        }

        void AddPacket(Packet packet)
        {
            lock (FilePackets)
                FilePackets.Add(packet.Offset, packet);

            Log.Add(Log.LogTypes.queueAddPacket, packet);

            newPacketEvent.Set();
        }

        bool MayHaveLocalData()
        {
            lock (FilePackets)
                return FilePackets.Any(x => x.Value.MayHaveLocalData);
        }

        internal bool Seek(long position)
        {
            int newOffset = 0;

            lock (FilePackets)
            {
                for (var i = 0; i < FilePackets.Count(); i++)
                {
                    newOffset = i;

                    if (FilePackets[i].Offset * pParameters.packetSize > position)
                        break;

                }

                if (newOffset != FilePackets.Count())
                {
                    dequeueOffset = newOffset;

                    return false;
                }
            }

            return true;
        }


        public void Dispose()
        {
            Log.Add(Log.LogTypes.queueFileDisposed, this);

            this.Cancel = true;

            //lock (FilePackets)
            //    FilePackets.Clear();
        }
    }

}
