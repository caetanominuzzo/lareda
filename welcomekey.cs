using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Globalization;

public class Program
{
	public static void Main()
	{

		IPEndPoint IPEndPoint = CreateIPEndPoint("127.0.0.1:15757");

		Console.WriteLine(
ToBase64String(
            IPEndPoint.Address.GetAddressBytes().Concat(
                BitConverter.GetBytes((UInt16)IPEndPoint.Port)).ToArray()
            )

         );

		Console.ReadKey();

	}

	public static IPEndPoint CreateIPEndPoint(string endPoint)
	{
	    string[] ep = endPoint.Split(':');
	    if(ep.Length != 2) throw new FormatException("Invalid endpoint format");
	    IPAddress ip;
	    if(!IPAddress.TryParse(ep[0], out ip))
	    {
	        throw new FormatException("Invalid ip-adress");
	    }
	    int port;
	    if(!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
	    {
	        throw new FormatException("Invalid port");
	    }
	    return new IPEndPoint(ip, port);
	}

	 public static string ToBase64String(byte[] term)
        {
            return term == null ? null : Convert.ToBase64String(term).Replace('/', '_').Replace('+', '-').Replace('=','=');
        }
}