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
        public static string filters_ = null;//"jBx-OWAanol4XmecG1R9hqLDVIrfHDllnS9vwBnejy0=";

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

            Add         = Ever << 1,
            Get         = Ever << 2,
            Complete    = Ever << 3,
            Arrived     = Ever << 4,
            Expire      = Ever << 5,
            Refresh     = Ever << 6,
            Seek        = Ever << 7,
            Ready       = Ever << 8,

            Read        = Ever << 9,
            Write       = Ever << 10,
            Start       = Ever << 11,
            Configure   = Ever << 12,
            Stop        = Ever << 13,
            Close 		= Ever << 14,
            Incoming    = Ever << 15,
            Outgoing 	= Ever << 16,

            Packets 	= Ever << 20,
            Metapackets = Ever << 21,
            Hash 		= Ever << 22,
            Peers 		= Ever << 23,
            File 		= Ever << 24,


            Application = Ever << 30,
            P2p 		= Ever << 31,
            Queue       = Ever << 32,
            Journaling  = Ever << 40,
            Stream      = Ever << 41,
            WebServer   = Ever << 42,
            Search      = Ever << 43,


            p2pIncoming 		    = P2p | Incoming,
            p2pOutgoing 		    = P2p | Outgoing,

            p2pIncomingPackets 		= p2pIncoming | Packets,
            p2pIncomingMetapackets 	= p2pIncoming | Metapackets,
            p2pIncomingHash 		= p2pIncoming | Hash,
            p2pIncomingPeers 		= p2pIncoming | Peers,

            p2pOutgoingPackets 		= p2pOutgoing | Packets,
            p2pOutgoingMetapackets 	= p2pOutgoing | Metapackets,
            p2pOutgoingHash 		= p2pOutgoing | Hash,
            p2pOutgoingPeers 		= p2pOutgoing | Peers,
            
            queueAddFile 		    = Queue | Add | File,
            queueFileComplete 		= Queue | Complete | File,
            queueAddPacket 		    = Queue | Add | Packets,
            queueExpireFile 		= Queue | Expire | File,
            queueGetPacket 		    = Queue | Get | Packets,
            queuePacketArrived 		= Queue | Arrived | Packets,


            queueFileReady    		= Queue | Ready | File,
            queueRefresh 	        = Queue | Refresh,

            journalingWrite 		= Journaling | Write,
            journalingRead 		    = Journaling | Read,


            streamSeek 		        = Stream | Seek,
            streamWrite 		    = Stream | Write,
            streamOutputClose 		= Stream | Outgoing | Close,
            streamInputClose 		= Stream | Incoming | Close,

            WebServerGet            = WebServer | Get,

            Nears 		= Ever << 61,
            All 		= Ever << 62,
            Only 		= Ever << 63
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

            lock ("data.txt")
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
            if (filter == LogTypes.None || (filter != LogTypes.All && type != LogTypes.Ever && (filter & type) != type))
                return;

            if (filter == LogTypes.Only && type != LogTypes.Only)
                return;


            //lock ("data.txt")
            {
                var s = data[0].ToString();

                log.Enqueue(s);

                var i = new LogItem(type, data);

                var json = string.Empty;

                lock (data)
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None);

                if (filters_ != null && !json.Contains(filters_))
                    return;

                //Log.Write(type + "\r\n\r\n" + json + "\r\n\r\n----------------------------------------------------------------------------\r\n");

                Log.Write(type + "\t" + json + "\t\r\n");

                return;

                if (OnLog != null)
                    OnLog(i);

                Items.Add(i);

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
