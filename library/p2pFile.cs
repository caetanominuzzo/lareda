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

        internal ManualResetEvent packetEvent = new ManualResetEvent(false);

        int dequeueFilePacketsOffset;

        int dequeueOffset;

        public int Levels = 0;

        public bool Success = false;

        public bool Cancel = false;

        Packet Root;

        Packet First;

        Packet SecondLast;

        Packet Last;

        List<p2pContext> Context = new List<p2pContext>();


        public IEnumerable<Guid> RequestTraceIdentifier
        {
            get
            {
                lock (Context)
                    return Context.Select(x => x.HttpContext.Request.RequestTraceIdentifier);
            }
        }

        internal enum FileStatus
        {
            addressStructureIncomplete = 0,
            addressStructureComplete = 1,
            dataStructureComplete = 2,
            dataComplete = 3

        }

        FileStatus status = FileStatus.addressStructureIncomplete;

        internal FileStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                if ((value == FileStatus.dataStructureComplete || value == FileStatus.dataComplete))
                //&&                    (status != FileStatus.dataStructureComplete && status != FileStatus.dataComplete))
                {
                    Log.Add(Log.LogTypes.queueDataStructureComplete, this.Filename);

                    Client.DownloadComplete(Address, Filename, SpecifFilename);
                }

                status = value;
            }
        }

        internal void AddContext(p2pContext context)
        {
            lock (Context)
            {
                if(!Context.Contains(context))
                    Context.Add(context);
            }
        }

        internal p2pFile(byte[] address, p2pContext context, string filename = null, string specifFile = null)
        {
            Address = address;

            Filename = filename;

            lock (Context)
                Context.Add(context);

            

            if (!string.IsNullOrEmpty(specifFile))
                SpecifFilename = specifFile;

            Root = AddPacket(address, null, 0, 0, filename);

            Thread thread = new Thread(Refresh);

            thread.Start();
        }

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
                            if (!c.HttpContext.Response.OutputStream.CanWrite)
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

        bool GotLastPacket = false;

        bool isNear(long bytesPosition)
        {
            return true;

            try
            {
                foreach (var c in this.Context)
                    Log.Add(Log.LogTypes.Ever, new { NEARS = 1, c.HttpContext.Request.RequestTraceIdentifier, Near = Math.Abs(c.OutputStreamPosition - bytesPosition) < pParameters.QueueWebserverStreamMaxDistance, c.OutputStreamPosition, bytesPosition, diff = c.OutputStreamPosition - bytesPosition, pParameters.QueueWebserverStreamMaxDistance, Filename, c.Download });

                lock (this.Context)
                    return this.Context.Any(x => Math.Abs(x.OutputStreamPosition - bytesPosition) < pParameters.QueueWebserverStreamMaxDistance);

                
            }
            catch { }

            return false;
        }

        void Refresh()
        {
            while (!Client.Stop && !Ended && AnyContext())
            {
                Log.Add(Log.LogTypes.Ever, new { REFRESH = 1, Filename, dequeueOffset });

                Packet packet = null;

                Log.Add(Log.LogTypes.queueRefresh, this);

                var wait = false;

                var count = 0;

                var max = 0;

                dequeueOffset = dequeueFilePacketsOffset - ((Math.Max(0, Levels - 1) * (pParameters.packetSize / pParameters.addressSize)) + Levels);

                var bytesPosition = dequeueOffset * pParameters.packetSize;

                lock (FilePackets)
                {
                    count = FilePackets.Count();

                    max = FilePackets.Keys.Max();
                    
                    wait = dequeueFilePacketsOffset == max + 1 || count == 0 || !isNear(bytesPosition);
                }

                if (wait)
                {
                    packetEvent.Reset();

                    var newEvent = packetEvent.WaitOne(pParameters.restart_requesting_packets_from_coda_timeout);

                    if (!newEvent)
                        continue;

                    if (!AnyContext())
                        break;

                    if (isNear(bytesPosition))
                    {
                        p2pContext c = null;

                        lock (Context)
                            c = this.Context.FirstOrDefault(x => Math.Abs(x.OutputStreamPosition - bytesPosition) < pParameters.QueueWebserverStreamMaxDistance);

                        dequeueOffset = (int)(c.OutputStreamPosition / pParameters.packetSize);

                        dequeueFilePacketsOffset = dequeueOffset + Levels; //todo:levels 2 & 3
                    }
                    else
                    {
                        lock (FilePackets)
                        {
                            for (var i = FilePackets.Count() - 1; i >= 0; i--)
                                if (FilePackets.ElementAt(i).Value.Arrived)
                                    FilePackets.RemoveAt(i);
                        }

                        continue;
                    }
                }

                lock (FilePackets)
                    if (dequeueFilePacketsOffset == 2 && !GotLastPacket)
                    {
                        packet = FilePackets[FilePackets.Keys.Max()];

                        GotLastPacket = true;
                    }
                    else
                    {
                        if (!FilePackets.Keys.Contains(dequeueFilePacketsOffset))
                            dequeueFilePacketsOffset = FilePackets.Keys.Min();

                        packet = FilePackets[dequeueFilePacketsOffset];

                        dequeueFilePacketsOffset = Math.Min(FilePackets.Keys.Max()  + 1 , dequeueFilePacketsOffset + 1);
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


        internal Packet AddPacket(byte[] address, p2pFile.Packet parent, int filePacketsOffset, int offset, string filename = null)
        {
            Packet p = new Packet(this, parent, filePacketsOffset, offset, address, filename);

            AddPacket(p);

            return p;
        }

        void AddPacket(Packet packet)
        {
            lock (FilePackets)
                FilePackets.Add(packet.FilePacketOffset, packet);

            Log.Add(Log.LogTypes.queueAddPacket, packet);

            packetEvent.Set();
        }

        bool MayHaveLocalData()
        {
            lock (FilePackets)
                return FilePackets.Any(x => x.Value.MayHaveLocalData);
        }

        internal bool CanReadFromLocalStream(long position, int count, string request_id)
        {
            var old_dequeue = dequeueOffset;

            var old_filedequeue = dequeueFilePacketsOffset;


            lock (FilePackets)
            {
                var min_dequeue = (int)(position / pParameters.packetSize);

                var max_dequeue = (int)((position + count) / pParameters.packetSize);

                //todo 2 & 3
                
                if (Root.Children.Where(x => x.Value.Offset >= min_dequeue && x.Value.Offset <= max_dequeue).All(x => x.Value.Arrived))
                {
                    Log.Add(Log.LogTypes.stream, new { CAN_READ = "OK", dequeueOffset = old_dequeue + "->" + dequeueOffset, request_id });

                    packetEvent.Set();

                    return true;
                }

                var next = Root.Children.FirstOrDefault(x => x.Value.Offset >= min_dequeue && x.Value.Offset <= max_dequeue && !x.Value.Arrived);


                dequeueOffset = next.Value.Offset;

                dequeueFilePacketsOffset = dequeueOffset + Levels; //todo:levels 2 & 3

                Log.Add(Log.LogTypes.stream, new { CAN_READ = "NOPE", dequeueOffset = old_dequeue + "->" + dequeueOffset, request_id });

                //if (!FilePackets.Keys.Contains(dequeueFilePacketsOffset) || FilePackets[dequeueFilePacketsOffset].Arrived)
                //{
                //    packetEvent.Set();

                //    return true;
                //}

            }

            packetEvent.Set();

            return false;
        }


        public void Dispose()
        {
            Log.Add(Log.LogTypes.queueFileDisposed, this);

            this.Cancel = true;

            this.packetEvent.Set();

            //lock (FilePackets)
            //    FilePackets.Clear();
        }
    }

}
