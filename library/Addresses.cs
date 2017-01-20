using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public static class Addresses
    {
        internal static byte[] zero
        {
            get
            {
                return new byte[32];
            }
        }

        //public static byte[] CompactAddressRequest(byte[] address, byte[] peerAddress = null)
        //{
        //    //addr jumps bytes
        //    if (peerAddress == null)
        //        return new byte[1].Concat(address).ToArray();


        //    byte diff = (byte)Addresses.Compare(address.ToArray(), peerAddress);

        //    byte simm = Convert.ToByte(Parameters.addressSize - diff);

        //    return new byte[] { simm }.Concat(address.Skip(simm).Take(address.Count() - simm)).ToArray();
        //}

        //public static byte[] InflateAddressRequest(byte[] address)
        //{
        //    if (address.Length == 0)
        //        return address;

        //    byte[] result = new byte[address[0] + address.Count() - 1];

        //    Client.localPeer.Address.Take(address[0]).ToArray().CopyTo(result, 0);

        //    address.Skip(1).Take(address.Length - 1).ToArray().CopyTo(result, address[0]);

        //    return result;
        //}

        public static bool Equals(byte[] s1, byte[] s2, bool full = false)
        {
         //   if (s1 == null || s2 == null)
         //       return false;
            //return s1 == s2;

            if (!full)
                return s1 == s2;

            if (s1 == s2)
                return true;

          //  if (s1.Length != s2.Length)
          //      return false;


            //var b =s1.SequenceEqual(s2);
 
            //if(b)
            //    s1 = s2;

            //return b;

          

            for (int i = 0; i < pParameters.addressSize; i++)
            {
                if (s1[i] != s2[i])
                    return false;

            }

            s1 = s2;

            return true;
        }


        internal static int Compare(byte[] x, byte[] y)
        {
            if (x == y)
                return 0;

            for (int i = 0; i < pParameters.addressSize; i++)
            {
                var diff = x[i] - y[i];

                if (diff != 0)
                    return diff;
            }

            return 0;
        }

        internal static double EuclideanDistance(byte[] x, byte[] y)
        {
            if (x==null || y == null || x.Length != y.Length || y.Length != pParameters.addressSize)
                return 514;

            double sum = 0;

            for (var i = 0; i < pParameters.addressSize; i++)
            {
                double diff = Math.Abs(x[i] - y[i]);

                sum += Math.Pow(Math.Min(diff, 256 - diff), 2);
            }

            sum = Math.Sqrt(sum);

            return sum;
        }

        internal static double Distance(byte[] s1, byte[] s2)
        {
            if (s1 == null || s2 == null)
                return pParameters.addressSize;

            if (s1 == s2)
                return 0;

            int diff = 0;
            int simm = 0;

            for (int i = 0; i < pParameters.addressSize; i++)
            {
                if (s1[i] != s2[i])
                {
                    diff = Math.Abs(s1[i] - s2[i]);
                    break;
                }

                simm++;
            }

            if (simm == pParameters.addressSize && s2.Length == s1.Length)
            {
                s2 = s1;
            }

            return pParameters.addressSize - simm + ((double)diff / 1000);
        }

        internal static IPEndPoint FromBytes(byte[] endPoint)
        {
            if (endPoint[0] != 0)
                return new IPEndPoint(
                        new IPAddress(endPoint.Take(4).ToArray()),
                        BitConverter.ToUInt16(endPoint, pParameters.ipv4Addresssize));
            
            return null;
        }

        public static byte[] ToBytes(IPEndPoint IPEndPoint)
        {
            return IPEndPoint.Address.GetAddressBytes().Concat(
                BitConverter.GetBytes((UInt16)IPEndPoint.Port)).ToArray();
        }

        internal static List<byte[]> ToAddresses(IEnumerable<byte> data)
        {
            int offset = 0;

            List<byte[]> result = new List<byte[]>();

            int count = data.Count();

            while (offset * pParameters.addressSize < count)
            {
                byte[] b = data.Skip(offset * pParameters.addressSize).
                    Take(pParameters.addressSize).
                    ToArray();

                offset++;

                if(b.Length == pParameters.addressSize)
                    result.Add(b);
            }

            return result;
        }

        internal static Dictionary<byte[], string> ToDirectories(byte[] data)
        {
            Dictionary<byte[], string> result = new Dictionary<byte[], string>();

            int offset = 0;

            while (offset < data.Count())
            {
                byte[] filename = Utils.ReadBytes(data, offset);

                offset += 4 + filename.Length;

                byte[] addr = data.Skip(offset).Take(pParameters.addressSize).ToArray();

                offset += pParameters.addressSize;

                if (addr.Length == pParameters.addressSize)
                    result.Add(addr, Encoding.Unicode.GetString(filename));
            }

            return result;
        }

    }
}
