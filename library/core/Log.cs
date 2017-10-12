using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace library
{
    public static class Log
    {//null;//
        public static string textFilter = null;//"TIXC_X2vlUeUQ3QBW8-cv_I";//"3DnFpsP2xPTUm9L4G--gLseAGI6QBqGWA5YKLUIHEoU=";
        // LogTypes.None;// LogTypes.Journaling | LogTypes.Stream | LogTypes.WebServer | LogTypes.File 
        ////
        public static LogTypes typeFilter = LogTypes.All;// LogTypes.P2p | LogTypes.WebServer | LogTypes.Queue;// | LogTypes.Queue;// | LogTypes.Journaling | LogTypes.Stream | LogTypes.File | LogTypes.Queue; //LogTypes.All; // LogTypes.Queue ;// | LogTypes.streamSeek | LogTypes.DownloadDispose | LogTypes.streamOutputClose;// LogTypes.None ;// Log.LogTypes.queueFileComplete | LogTypes.WebServerGet | LogTypes.queueGetPacket | LogTypes.Nears; // LogTypes.queue;// | LogTypes.Application | LogTypes.p2pIncomingPackets | LogTypes.p2pOutgoingPackets;

        public static LogOperations OpFilter = LogOperations.Any;

        internal static LogOperations FromCommand(RequestCommand command)
        { 
            switch (command)
            {
                case RequestCommand.Packet: return LogOperations.Packets;
                case RequestCommand.Metapackets: return LogOperations.Metapackets;
                case RequestCommand.Hashs: return LogOperations.Hash;
                case RequestCommand.Peer: return LogOperations.Peers;
            }

            return LogOperations.None;
        }

        [Flags]
        public enum LogTypes : UInt64
        {
            None = 0,

            Ever = 1,

         
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

            ClosingMaxDownloadSize = Any << 29,


            Packets = Any << 30,
            Metapackets = Any << 31,
            Hash = Any << 32,
            Peers = Any << 33,
            File = Any << 34,

            CantSeek = Cant | Seek,

            CantRead = Cant | Read,

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

        internal static Regex regex = null;

        public static void Write(string s)
        {
            log.Debug(s);

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
#if !DEBUG
         //   return;
#endif

            //return;

            if (typeFilter == LogTypes.None || (typeFilter != LogTypes.All && type != LogTypes.Ever && (typeFilter & type) != type))
                return;

            if (typeFilter == LogTypes.Only && type != LogTypes.Only)
                return;

            if (OpFilter == LogOperations.None || (OpFilter != LogOperations.Any && operation != LogOperations.Any && (OpFilter & operation) != operation))
                return;

            if(null == regex && null != textFilter)
                new Regex("\\\"[^\"]*?" + textFilter + "[^\\\"]*?\\\"");

            //lock ("data.txt")
            {
                var s = data[0].ToString();

                //log.Enqueue(s);

                var i = new LogItem(type, data);

                var json = string.Empty;

                try
                {
                    lock (data)
                        json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None);
                }
                catch (Exception e)
                {
                    json = e.ToString();
                }

                if (textFilter != null && !json.Contains(textFilter))
                    return;

                if(null != regex)
                    json = regex.Replace(json, string.Empty);

                //Log.Write(type + "\r\n\r\n" + json + "\r\n\r\n----------------------------------------------------------------------------\r\n");

                Log.Write("\t" + type.ToString().PadRight(10) + "\t" + operation.ToString().PadRight(10) + "\t" + json + "\t\r\n");

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
