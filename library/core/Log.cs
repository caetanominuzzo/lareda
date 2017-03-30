using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public static class Log
    {
        public static LogTypes filter = LogTypes.stream | LogTypes.queue;// | LogTypes.Application | LogTypes.p2pIncomingPackets | LogTypes.p2pOutgoingPackets;
        internal static LogTypes FromCommand(RequestCommand command)
        {
            switch (command)
            {
                case RequestCommand.Packet: return LogTypes.Packets;
                case RequestCommand.Metapackets: return LogTypes.Metapackets;
                case RequestCommand.Hashs: return LogTypes.Hash;
                case RequestCommand.Peer: return LogTypes.Peers;
            }

            return LogTypes.None;
        }
        
        [Flags]
        public enum LogTypes : UInt64
        {
            None = 0,

            Ever = 1,

            Application = 1 << 1,

            Read   =1 << 4,
            Write = 1 << 5,
            Start = 1 << 6,
            Configure = 1 << 7,
            Stop = 1 << 8,
            Close = 1 << 9,


            p2p = 1 << 10,

            Packets = 1 << 11,
            Metapackets = 1 << 12,
            Hash = 1 << 13,
            Peers = 1 << 14,

            Incoming = 1 << 15,
            Outgoing = 1 << 16,

            p2pIncoming = p2p | Incoming,
            p2pOutgoing = p2p | Outgoing,

            p2pIncomingPackets = p2pIncoming | Packets,
            p2pIncomingMetapackets = p2pIncoming | Metapackets,
            p2pIncomingHash = p2pIncoming | Hash,
            p2pIncomingPeers = p2pIncoming | Peers,

            p2pOutgoingPackets = p2pOutgoing | Packets,
            p2pOutgoingMetapackets = p2pOutgoing | Metapackets,
            p2pOutgoingHash = p2pOutgoing | Hash,
            p2pOutgoingPeers = p2pOutgoing | Peers,

            //Journaling

            //Queue
            queueAddFile = 1 << 22,
            queueFileComplete = 1 << 23,
            queueAddPacket = 1 << 24,
            queueEndOfPackets = 1 << 25,
            queueExpireFile = 1 << 26,
            queueFileDisposed = 1 << 27,
            queueLastPacketTimeout = 1 << 28,
            queueGetPacket = 1 << 29,
            queuePacketArrived = 1 << 30,

            queue = queueAddFile | queueFileComplete | queueAddPacket | queueEndOfPackets | queueExpireFile | queueFileDisposed | queueLastPacketTimeout | queuePacketArrived | queueDataStructureComplete,
            
            All = Ever << 31,

            queueDataStructureComplete = Ever << 32,
            queueRefresh = Ever << 33,

            //SearchResult

            journaling = Ever << 40,

            journalingWrite = journaling | Write,
            journalingRead = journaling | Read,


            //Stream
            stream  = Ever << 41,
            seek    = Ever << 42,
            streamSeek = stream | seek,
            streamWrite = stream | Write,
            streamOutputClose = stream | Outgoing | Close,
            streamInputClose = stream | Incoming| Close,



        }

        

        static Queue<string> log = new Queue<string>();

        public delegate void LogHandler(LogItem item);

        public static event LogHandler OnLog;


        internal static void Clear()
        {
            File.WriteAllText("data.txt", string.Empty);
        }

        public static void Write(string s)
        {
            s = DateTime.Now.ToString("HH:mm:ss.fff") + " \t" + s + "\r\n";

            lock("data.txt")
                File.AppendAllText("data.txt", s);
        }

        public class LogItem
        {
            public DateTime DateTime;

            public LogTypes Type;

            public object[] Data;

            internal LogItem(LogTypes type, params object[] data)
            {
                Type = type;

                Data = data;

                DateTime = DateTime.Now;
            }
        }

        public static List<LogItem> Items = new List<LogItem>();
        

        public static void Add(LogTypes type, params object[] data)
        {
            if ((type & LogTypes.queue) != LogTypes.None)
            {

            }

            if (filter != LogTypes.All && type != LogTypes.Ever && (filter & type) == LogTypes.None)
                return;

            lock ("data.txt")
            {
                var s = data[0].ToString();

                log.Enqueue(s);

                var i = new LogItem(type, data);

                if (OnLog != null)
                    OnLog(i);

                Items.Add(i);

                if (log.Count() > 2000)
                    log.Dequeue();

                var ss = s.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                StringBuilder sb = new StringBuilder();

                //   if(tabs == 0)
                //     sb.AppendLine(DateTime.Now.ToString());

                foreach (var sss in ss)
                {
                    sb.AppendLine(sss + ']');
                }

                //write(sb.ToString() + Environment.NewLine);
            }
        }

        private static char readPenultimoChar()
        {
            lock ("data.txt")
                try
                {
                    using (var s = File.OpenRead("data.txt"))
                    {
                        var size = s.Length;

                        s.Seek(-1, SeekOrigin.End);

                        return (char)s.ReadByte();
                    }
                }
                catch
                {
                    return '0';
                }
        }
    }
}
