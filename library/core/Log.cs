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
        [Flags]
        public enum LogTypes
        {
            None = 0,

            Ever = 1,

            InterfaceException          = 1 << 1,


            //p2p
            p2pIncomingHash             = 1 << 2,
            p2pIncomingMetapackets      = 1 << 3,
            p2pIncomingPackets          = 1 << 4,
            p2pIncomingPeers            = 1 << 5,
            p2pIncomingException         = 1 << 6,
            p2pIncoming                 = p2pIncomingHash | p2pIncomingMetapackets | p2pIncomingPackets | p2pIncomingPeers | p2pIncomingException,

            p2pOutgoingHash = 1 << 7,
            p2pOutgoingMetapackets = 1 << 8,
            p2pOutgoingPackets = 1 << 9,
            p2pOutgoingPeers = 1 << 10,
            p2pOutgoingException = 1 << 11,
            p2pOutgoing = p2pOutgoingHash | p2pOutgoingMetapackets | p2pOutgoingPackets | p2pOutgoingPeers | p2pOutgoingException,

            //Journaling

            //Queue
            queueAddFile                = 1 << 12,
            queueFileComplete           = 1 << 13,
            queueAddPacket              = 1 << 14,
            queueEndOfPackets           = 1 << 15,
            queueExpireFile             = 1 << 16,
            queueFileDisposed           = 1 << 17,
            queueLastPacketTimeout      = 1 << 18,
            queueGetPacket              = 1 << 19,
            queuePacketArrived          = 1 << 20,

            queue = queueAddFile | queueFileComplete | queueAddPacket | queueEndOfPackets | queueExpireFile | queueFileDisposed | queueLastPacketTimeout | queueGetPacket | queuePacketArrived,



            All = 1 << 30
        }

        static LogTypes filter = (LogTypes.queue | LogTypes.InterfaceException | LogTypes.p2pIncomingPackets | LogTypes.p2pOutgoingPackets);

        static Queue<string> log = new Queue<string>();

        static bool enableStopMotion = false;

        internal static void Clear()
        {
            File.WriteAllText("data.txt", string.Empty);
        }

        static void write(string s)
        {
            s = DateTime.Now.ToString("HH:mm:ss.fff") + " \t" + s;

            File.AppendAllText("data.txt",  s);
        }

        public static void Write(string s, LogTypes type, int tabs = 0)
        {
            if (tabs < 0)
                return;

            if((type & LogTypes.queue) != LogTypes.None)
            {

            }

            if (filter != LogTypes.All && type != LogTypes.Ever && (filter & type) == LogTypes.None)
                return;

            lock ("data.txt")
            {
                var c = readPenultimoChar();

                if (c == 'p')
                {
                    enableStopMotion = true;
                    write("ause. Stop motion enabled");
                    
                }

                

                if (enableStopMotion)
                {
                    c = readPenultimoChar();

                    while (c != '.' && c != 'g')
                    {
                        System.Threading.Thread.Sleep(1000);

                        c = readPenultimoChar();
                    }

                    if (c == 'g')
                    {
                        enableStopMotion = false;
                        write("o! Stop motion disabled");
                    }

                }

                //if (log.Contains(s))
                //    return;

                log.Enqueue(s);

                if (log.Count() > 2000)
                    log.Dequeue();

                var ss = s.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                StringBuilder sb = new StringBuilder();

                //   if(tabs == 0)
                //     sb.AppendLine(DateTime.Now.ToString());

                foreach (var sss in ss)
                {
                    sb.AppendLine(string.Empty.PadLeft(tabs, '\t') + sss + ']');
                }

                write( sb.ToString() + Environment.NewLine);
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
