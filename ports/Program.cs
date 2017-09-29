using library;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ports
{
    class Program
    {
        static void Main(string[] args)
        {
            var s = Console.ReadLine();

            var port = 0;

            if(int.TryParse(s, out port))
            {
                Console.WriteLine(Utils.Points(BitConverter.GetBytes((UInt16)port)));

                Console.ReadKey();
            }
            else
            {
                var ip = CreateIPEndPoint(s);

                Console.WriteLine(Utils.ToBase64String(Addresses.ToBytes(ip)));

                Console.ReadKey();
            }

            
        }

        static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            int port;
            if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }
    }
}
