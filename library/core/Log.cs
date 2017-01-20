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
        static Queue<string> log = new Queue<string>();

        static bool enableStopMotion = false;

        static void write(string s)
        {
            File.AppendAllText("data.txt", s);
        }

        public static void Write(string s, int tabs = 0)
        {
            if (tabs < 10)
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

                if (log.Contains(s))
                    return;

                log.Enqueue(s);

                if (log.Count() > 2)
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
