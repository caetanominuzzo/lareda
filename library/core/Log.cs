﻿using log4net;
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
        public static string textFilter =  "N6t4s0kNFXjD";

        public static LogTypes typeFilter = LogTypes.Stream;// | LogTypes.WebServer | LogTypes.Queue;// LogTypes.WebServerGet | LogTypes.streamSeek | LogTypes.DownloadDispose | LogTypes.streamOutputClose;// LogTypes.None ;// Log.LogTypes.queueFileComplete | LogTypes.WebServerGet | LogTypes.queueGetPacket | LogTypes.Nears; // LogTypes.queue;// | LogTypes.Application | LogTypes.p2pIncomingPackets | LogTypes.p2pOutgoingPackets;

        public static LogOperations OpFilter = LogOperations.Paint;

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

            Packets 	= Ever << 01,
            Metapackets = Ever << 02,
            Hash 		= Ever << 03,
            Peers 		= Ever << 04,
            File 		= Ever << 05,
            KeepAlive   = Ever << 06,
            Download    = Ever << 07,


            Application = Ever << 08,
            P2p 		= Ever << 09,
            Queue       = Ever << 10,
            Journaling  = Ever << 11,
            Stream      = Ever << 12,
            WebServer   = Ever << 13,
            Search      = Ever << 14,

            StreamKeepAlive = Stream | KeepAlive,

            
            All 		= Ever << 62,
            Only 		= Ever << 63
        }


        [Flags]
        public enum LogOperations : UInt64
        {
            None = 0,

            Any = 1,

            Add = Any << 1,
            Get = Any << 2,
            Complete = Any << 3,
            Arrived = Any << 4,
            Expire = Any << 5,
            Refresh = Any << 6,
            Seek = Any << 7,
            Ready = Any << 8,

            Read = Any << 9,
            Write = Any << 10,
            Start = Any << 11,
            Configure = Any << 12,
            Stop = Any << 13,
            Close = Any << 14,
            Incoming = Any << 15,
            Outgoing = Any << 16,
            Serialize = Any << 17,

            Dispose = Any << 18,

            Open = Any << 19,

            Exception = Any << 20,

            IsNear = Any << 21,
            TimeOut = Any << 22,

            Header = Any << 23,

            ClosingInitialRequest = Any << 24,
            ClosingContext = Any << 25,

            ClosingResponse = Any << 26,

            Install = Any << 27,

            Paint = Any << 28,

            CantSeek = Cant | Seek,

            Cant = Any << 61,
            All = Any << 62
        }

        public delegate void LogHandler(LogItem item);

        public static event LogHandler OnLog;


        internal static void Clear()
        {
            File.WriteAllText("data.txt", string.Empty);
        }

        internal static ILog log = log4net.LogManager.GetLogger(typeof(Log));

        public static void Write(string s)
        {
#if DEBUG
            log.Debug(s);
#endif

            //s = DateTime.Now.ToString("HH:mm:ss.fff") + " \t" + s + "\r\n";

            //lock ("data.txt")
            //  s = string.Empty;
            //File.AppendAllText("data.txt", s);
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


        public static void Add(LogTypes type, LogOperations operation, params object[] data)
        {
            if (typeFilter == LogTypes.None || (typeFilter != LogTypes.All && type != LogTypes.Ever && (typeFilter & type) != type))
                return;

            if (typeFilter == LogTypes.Only && type != LogTypes.Only)
                return;

            if (OpFilter == LogOperations.None || (OpFilter != LogOperations.Any && operation != LogOperations.Any && (OpFilter & operation) != operation))
                return;

            //lock ("data.txt")
            {
                var s = data[0].ToString();

                //log.Enqueue(s);

                var i = new LogItem(type, data);

                var json = string.Empty;

                lock (data)
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None);

                if (textFilter != null && !json.Contains(textFilter))
                    return;

                //Log.Write(type + "\r\n\r\n" + json + "\r\n\r\n----------------------------------------------------------------------------\r\n");

                Log.Write(type + "\t" + operation + "\t" + json + "\t\r\n");

                return;

                OnLog?.Invoke(i);

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
