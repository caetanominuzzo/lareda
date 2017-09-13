using log4net;
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
    public partial class p2pFile : IDisposable
    {
        internal byte[] Address;

        public string Filename;

        internal string SpecifFilename = string.Empty;

        SortedList<int, Packet> FilePackets = new SortedList<int, Packet>();

        List<int> arrives = new List<int>();

        public int FirstContentFilePacketOffset = 0;

        List<Packet> FilePacketsArrived = new List<Packet>();


        internal static ILog log = log4net.LogManager.GetLogger(typeof(p2pFile));

        public int[] Cursors
        {
            get
            {
                return this.Context.Select(x => Convert.ToInt32(x.OutputStreamPosition / pParameters.packetSize)).ToArray();
            }
        }

        public int[] Arrives
        {
            get
            {
                int[] result = null;

                lock (arrives)
                    result = arrives.ToArray();

                return result;
            }
        }

        public long[] Complete()
        {
            if (Monitor.IsEntered(FilePackets))
            {
                log.Debug("Monitor is entered");

                return new long[] { -1, 0, 0 };
            }

            if (Status < FileStatus.addressStructureComplete)
            {
                log.Debug("Structure incomplete");

                return new long[] { -1, 0, 0 };
            }





            int[] keys = null;

            lock (FilePackets)
                keys = FilePackets.Keys.ToArray();

            var result = new List<long>();


            result.Add(keys[keys.Length - 2] * pParameters.packetSize + FilePackets[keys.Last()].data.Length);

            log.Debug("Structure Complete \t" + this.Filename + "\t" + result[0].ToString());

            foreach (var i in keys)
            {
                if (FilePackets[i].PacketType != PacketTypes.Content)
                    continue;

                if (FilePackets[i].Arrived)
                {
                    result.Add(FilePackets[i].Offset * pParameters.packetSize);

                    result.Add(pParameters.packetSize);
                }
            }

            return result.ToArray();
        }

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

        public List<p2pContext> Context = new List<p2pContext>();


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
                //&&             (status != FileStatus.dataStructureComplete && status != FileStatus.dataComplete))
                {
                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.Ready, this.Filename);
                }

                status = value;
            }
        }

        internal void AddContext(p2pContext context)
        {
            lock (Context)
            {
                if (!Context.Contains(context))
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

                return Context.Any();
            }


        }

        internal bool GetLastPacket = false;

        internal bool GetSecondLastPacket = false;

        bool isNear(long bytesPosition)
        {
            return true;

            if (this.GetLastPacket || this.GetSecondLastPacket)
                return true;

            try
            {
                var result = false;

                lock (this.Context)
                    result = this.Context.Any(x => Math.Abs(x.OutputStreamPosition - bytesPosition) < pParameters.QueueWebserverStreamMaxDistance ||
                     Math.Abs(x.OutputStreamEndPosition - bytesPosition) < pParameters.QueueWebserverStreamMaxDistance);

                foreach (var c in this.Context)
                    Log.Add(Log.LogTypes.Stream, Log.LogOperations.IsNear, new { c, Near = result, OutputStreamPosition = c.OutputStreamPosition, bytesPosition, diff = c.OutputStreamPosition - bytesPosition, pParameters.QueueWebserverStreamMaxDistance, Filename, c.Download });

                return result;


            }
            catch { }

            return false;
        }

        void Refresh()
        {
            while (!Client.Stop && !Ended && AnyContext())
            {
                Log.Add(Log.LogTypes.Queue, Log.LogOperations.Refresh, new { Filename, dequeueOffset });

                Packet packet = null;

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
                    {
                        if (dequeueFilePacketsOffset == max + 1)
                            dequeueFilePacketsOffset = FilePackets.Keys.Min();

                        continue;
                    }
                    else
                        p2pFile.Queue.Reset(this);

                    if (!AnyContext())
                        break;

                    if (isNear(bytesPosition))
                    {
                        p2pContext c = null;

                        lock (Context)
                        {
                            c = this.Context.FirstOrDefault(x => Math.Abs(x.OutputStreamPosition - bytesPosition) < pParameters.QueueWebserverStreamMaxDistance);

                            if (c == null)
                                dequeueFilePacketsOffset = FilePackets.Keys.Min();
                            else
                                dequeueFilePacketsOffset = (int)(c.OutputStreamPosition / pParameters.packetSize) + FirstContentFilePacketOffset;
                        }

                        //dequeueOffset = FilePackets
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
                    if (GetLastPacket)
                    {
                        packet = FilePackets[FilePackets.Keys.Max()];

                        GetLastPacket = false;
                    }
                    else if (GetSecondLastPacket)
                    {
                        packet = FilePackets[FilePackets.Keys.Max() - 1];

                        GetSecondLastPacket = false;
                    }

                if (null == packet)
                {
                    lock (FilePackets)
                    {
                        packet = FilePackets.FirstOrDefault(x => x.Value.Level < Levels && !x.Value.Arrived).Value;

                        if (null == packet)
                        {
                            if (!FilePackets.Keys.Contains(dequeueFilePacketsOffset))
                                dequeueFilePacketsOffset = FilePackets.Keys.Min();

                            packet = FilePackets[dequeueFilePacketsOffset];

                            dequeueFilePacketsOffset = Math.Min(FilePackets.Keys.Max() + 1, dequeueFilePacketsOffset + 1);
                        }
                    }
                }

                packet.Get();



            }

            if (Success)
            {
                Queue.QueueComplete(this);


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

            Log.Add(Log.LogTypes.Queue, Log.LogOperations.Add, packet);

            packetEvent.Set();
        }

        bool MayHaveLocalData()
        {
            lock (FilePackets)
                return FilePackets.Any(x => x.Value.MayHaveLocalData);
        }

        internal bool CanReadFromLocalStream(long position, int count)
        {
            var old_dequeue = dequeueOffset;

            var old_filedequeue = dequeueFilePacketsOffset;

            if (Levels != 0 && FirstContentFilePacketOffset == 0)
                return false;

            lock (FilePackets)
            {
                var min_dequeue = (int)(position / pParameters.packetSize);

                var max_dequeue = (int)((position + count) / pParameters.packetSize);

                min_dequeue += FirstContentFilePacketOffset;

                max_dequeue += FirstContentFilePacketOffset;

                if (FilePackets.Where(x => x.Value.FilePacketOffset >= min_dequeue && x.Value.FilePacketOffset <= max_dequeue).All(x => x.Value.Arrived))
                {
                    packetEvent.Set();

                    return true;
                }

                var next = FilePackets.FirstOrDefault(x => x.Value.FilePacketOffset >= min_dequeue && x.Value.FilePacketOffset <= max_dequeue && !x.Value.Arrived);

                dequeueOffset = next.Value.Offset;

                dequeueFilePacketsOffset = next.Value.FilePacketOffset;

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
            this.Cancel = true;

            this.packetEvent.Set();

            //lock (FilePackets)
            //    FilePackets.Clear();
        }
    }

}
