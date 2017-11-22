using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tsockets
{
    class Program
    {

        static int port1 = 0;

        static int port2 = 0;

        static byte[] data = null;

        static Socket listener1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static Socket listener2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static IPAddress address = IPAddress.Parse("179.181.76.10");

        static IPEndPoint ip1 = null;

        static IPEndPoint ip2 = null;

        static List<byte[]> datas = null;

        static void Main(string[] args)
        {
            data = File.ReadAllBytes(@"D:\lareda\windows_desktop\bin\Debug\packets\3aR0xA5ZI-N3lXRnz9DpSCjdTfCGDTG1Cs1AtBQq9QE=");

            datas = new List<byte[]>();

            var m = 4000;

            for(var i = 0; i< 20;i++)
            {
                datas.Add(data.Take(m).ToArray());
                m++;
            }

            //var s = Console.ReadLine();

            port1 = 46002;// int.Parse(s);

            ip1 = new IPEndPoint(address, port1);

            var thread = new Thread(start1);

            thread.Start();


            //s = Console.ReadLine();

            port2 = 46003;// int.Parse(s);

            ip2 = new IPEndPoint(address, port2);

            var thread2 = new Thread(start2);

            thread2.Start();

            var s = "1";
            while (s != string.Empty)
            {

                var i = int.Parse(s);

                for (var j = 0; j < i; j++)
                {

                    write(listener1, ip2, j);
                   // write(listener2, ip1, j);
                }

                s = Console.ReadLine();
            }

        }

        static void start(Socket listener, int port)
        {
            listener.Bind(new IPEndPoint(IPAddress.Any, port));

            //            uint IOC_IN = 0x80000000;
            //            uint IOC_VENDOR = 0x18000000;
            //            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            //            listener.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false)
            //}, null);

            listener.Listen(100);


            while (true)
            {
                Console.WriteLine("Waiting for a connection...");

                var size = 102410;

                var buffer = new byte[size];

                var c = listener.Accept().Receive(buffer);

                Console.Write("<< " + c);
            }
        }

        static void write(Socket listener, IPEndPoint endpoint, int c)
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            s.Connect(endpoint);

            s.Send(datas[c]);

            s.Close();

            Console.WriteLine(">> " + endpoint.Port  + "\\t");
        }


        static void start1()
        {
            start(listener1, port1);

            
        }

        static void start2()
        {
            start(listener2, port2);

           
        }
    }
}
