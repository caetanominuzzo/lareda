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

        public string Base64Address
        {
            get
            {
                return Utils.ToBase64String(Address);
            }
        }

        public string Filename;

        internal string SpecifFilename = string.Empty;

        Dictionary<int, Packet> FilePackets = new Dictionary<int, Packet>();

        List<int> arrives = new List<int>();

        public int FirstContentFilePacketOffset = 0;

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

        internal double ReturnRatio = 1;

        internal ManualResetEvent stoppedEvent = new ManualResetEvent(false);

        internal ManualResetEvent packetEvent = new ManualResetEvent(false);

        int dequeueFilePacketsOffset;

        int dequeueOffset;

        public int Levels = 0;

        long length = -1;

        public long Length
        {
            get { return length; }
            set { length = value; }
        }

        public bool Success = false;

        public bool Cancel = false;

        Packet Root;

        public List<p2pContext> Context = new List<p2pContext>();

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
                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.Ready, this);
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

        internal p2pFile(byte[] address, p2pContext context, string filename = null, string specificFile = null)
        {
            Address = address;

            Filename = filename;

            lock (Context)
                Context.Add(context);



            if (!string.IsNullOrEmpty(specificFile))
                SpecifFilename = specificFile;

            Root = AddPacket(address, null, 0, 0, filename);

            Root.Get();

            Thread thread = new Thread(Refresh);

            //thread.Start();
        }

        bool Ended
        {
            get
            {
                lock (FilePackets)
                    return Cancel || Success;// || !FilePackets.Any() || FilePackets.All(x => x.Value.Arrived);
            }
        }

        bool AnyContext()
        {
            lock (Context)
                return Context.Any();
        }

        internal bool GetLastPacket = false;

        internal bool GetSecondLastPacket = false;

        internal ManualResetEvent gotLastsPackets = new ManualResetEvent(false);

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
                    Log.Add(Log.LogTypes.Stream, Log.LogOperations.IsNear, new { c, Near = result, OutputStreamPosition = c.OutputStreamPosition, bytesPosition, diff = c.OutputStreamPosition - bytesPosition, pParameters.QueueWebserverStreamMaxDistance, File = this, c.Download });

                return result;


            }
            catch { }

            return false;
        }

        void Refresh()
        {
            while (!Client.Stop && !Ended)// && AnyContext())
            {
                Log.Add(Log.LogTypes.Queue, Log.LogOperations.Refresh, this);

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

                    wait = (dequeueFilePacketsOffset == max + 1 || count == 0) && !isNear(bytesPosition);
                }

                if (wait)
                {
                    packetEvent.Reset();

                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.Refresh, new { WAITING = 1, File = this, dequeueOffset });

                    var newEvent = packetEvent.WaitOne(pParameters.restart_requesting_packets_from_coda_timeout);

                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.Refresh, new { NEWPACKETEVENT = newEvent, File = this, dequeueOffset });

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
                        //lock (FilePackets)
                        //{
                        //    for (var i = FilePackets.Count() - 1; i >= 0; i--)
                        //        if (FilePackets.ElementAt(i).Value.Arrived)
                        //            FilePackets.RemoveAt(i);
                        //}

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

                        //break;
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

            Log.Add(Log.LogTypes.Queue, Log.LogOperations.Close | Log.LogOperations.Refresh, new { File = this, dequeueOffset, Success });


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

            Log.Add(Log.LogTypes.Queue, Log.LogOperations.Add | Log.LogOperations.Packets, packet);

            packetEvent.Set();
        }

        bool MayHaveLocalData()
        {
            lock (FilePackets)
                return FilePackets.Any(x => x.Value.MayHaveLocalData);
        }

        internal int TryReadFromPackets(byte[] buffer, int position, int count, out Packet[] packets)
        {

            if (position > 0)
            {

            }

            Packet packet = null;

            var old_dequeue = dequeueOffset;

            var old_filedequeue = dequeueFilePacketsOffset;

            if (Levels != 0 && FirstContentFilePacketOffset == 0)
            {
                lock (FilePackets)
                    packet = FilePackets.First(x => x.Value.FilePacketOffset == 1).Value;

                packets = new Packet[] { packet };

                return -1;
            }

            if (position + count > Length)
                count = (int)Length - position;

            var min_dequeue = (int)(double)position / pParameters.packetSize;

            var max_dequeue = (int)((position + count) / pParameters.packetSize);

            //min_dequeue -= FirstContentFilePacketOffset;

            //max_dequeue -= FirstContentFilePacketOffset;

            IEnumerable<KeyValuePair<int, Packet>> vpackets = null;

            lock (FilePackets)
            {
                vpackets = FilePackets.Where(x => x.Value.PacketType == PacketTypes.Content && x.Value.Offset >= min_dequeue && x.Value.Offset <= max_dequeue && x.Value.Arrived).OrderBy(x => x.Value.Offset).ToArray();

                var packets_count = vpackets.Count();

                if ((max_dequeue - min_dequeue == 0 && packets_count == 1) || (packets_count >= max_dequeue - min_dequeue + 1))
                {
                    var mod_first_packet = (position % pParameters.packetSize);

                    var mod_last_packet = vpackets.Last().Value.data.Length - ((position + count) % pParameters.packetSize) - pParameters.packetHeaderSize;

                    var buffer_offset = 0;

                    var i = 0;

                    foreach (var p in vpackets)
                    {
                        var length = p.Value.data.Length - pParameters.packetHeaderSize;

                        var offset_first_packet = 0;

                        if (i == 0)
                        {
                            //mod_first_packet = (position % pParameters.packetSize) - (pParameters.packetSize - p.Value.data.Length);

                            length -= mod_first_packet;

                            offset_first_packet = mod_first_packet;
                        }

                        if (i == packets_count - 1)
                        {
                            length -= mod_last_packet;
                        }

                        if (length < 0)
                        {

                        }

                        Buffer.BlockCopy(p.Value.data, pParameters.packetHeaderSize + offset_first_packet, buffer, buffer_offset, length);

                        buffer_offset += length;

                        i++;
                    }

                    packet = null;

                    packets = new Packet[] { packet };

                    return buffer_offset;
                }
                else
                {
                    var nexts = FilePackets.Where(x => x.Value.Offset >= min_dequeue && x.Value.Offset <= max_dequeue && !x.Value.Arrived).OrderBy(x => x.Value.Offset).Take(10);

                    var next = nexts.FirstOrDefault();

                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.CantRead, this, position, min_dequeue, max_dequeue, next, old_dequeue);

                    if (null != next.Value)
                    {
                        dequeueOffset = next.Value.Offset;

                        dequeueFilePacketsOffset = next.Value.FilePacketOffset;

                        packet = next.Value;
                    }
                    else
                    {

                        Log.Add(Log.LogTypes.Queue, Log.LogOperations.CantRead, this, FilePackets.OrderBy(x => x.Value.Offset).Select(x => x.Value.FilePacketOffset.ToString() + (x.Value.Arrived ? "!" : "")));

                        Log.Add(Log.LogTypes.Queue, Log.LogOperations.CantRead, this, FilePackets.OrderBy(x => x.Value.Offset).Select(x => x.Value.Offset.ToString() + (x.Value.Arrived ? "!" : "")));

                        dequeueOffset = min_dequeue;

                        dequeueFilePacketsOffset = min_dequeue + FirstContentFilePacketOffset;

                        packet = null;
                    }

                    packets = nexts.Select(x => x.Value).ToArray();

                }
            }


            return -1;

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

                if (FilePackets.Where(x => x.Value.FilePacketOffset >= min_dequeue && x.Value.FilePacketOffset <= max_dequeue && x.Value.Arrived).Count() >= max_dequeue - min_dequeue + 1)
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
