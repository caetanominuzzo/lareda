#define SIMPLE //for metapackets print command
//#define PRINT //for print every search result

#if PRINT

using GraphVizWrapper;
using GraphVizWrapper.Commands;
using GraphVizWrapper.Queries;

#endif

using Exyll;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace library 
{
    public static class Utils
    {
        static MD5 MD5 = MD5.Create();

        static SHA256 SHA = SHA256.Create();

        static Crc16 CRC = new Crc16(Crc16Mode.Standard);

        public static Regex Base64ReplaceRegexPlus = null;

        public static Regex Base64ReplaceRegexSlash = null;


        internal static Random Rand = new Random();

        static string[] SimpleNames = new string[65536];

        static string[] SimpleAddress = null;

        internal static int AddressCount = 0;


        public static byte[] GetPseudoAddress()
        {
            return GetAddress(16);
        }

        public static int Random(int min, int max)
        {
            return Rand.Next(min, max);
        }

        public static string Replace(string input, string pattern, string replace)
        {
            Regex r = new Regex(pattern);

            return r.Replace(input, replace);
        }

        public  static byte[] GetAddress(int size = 0, byte[] append = null)
        {
            if (size == 0)
                size = pParameters.addressSize;

            byte[] result = new byte[size + (null != append? append.Length : 0)];
            
            Rand.NextBytes(result);

            //if(UseAddressCountForVirtualAttributes)
                Utils.ToAddressSizeArray(AddressCount++.ToString()).CopyTo(result, 0);

            if(null != append)
                append.CopyTo(result, size);

            return result;
        }

        static bool UseAddressCountForVirtualAttributes = true;

        public static int StopInternalAddressCount()
        {
            UseAddressCountForVirtualAttributes = false;

            return AddressCount;
        }

        public static string Points(IEnumerable<byte> data)
        {
            List<string> s = new List<string>();

            return BitConverter.ToString(data.ToArray()).Replace("-", " ");

            foreach (byte b in data)
            {
                s.Add(b.ToString().PadLeft(3, '0'));
            }

            return string.Join(".", s);
        }

        public static byte[] ComputeCRC(byte[] data)
        {
            return CRC.ComputeChecksumBytes(data);
        }

        public static int ComputeChecksum(byte[] data)
        {
            return CRC.ComputeChecksum(data);
        }

        public static byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            lock (SHA)
                return SHA.ComputeHash(buffer, offset, count);
        }

        public static byte[] ToAddressSizeArray(string value)
        {
            var bytes = Encoding.Unicode.GetBytes(value.ToUpper());

            return Hash(bytes);
        }

        public static byte[] Hash(byte[] bytes)
        {
            byte[] hash;

            lock (SHA)
                hash = SHA.ComputeHash(bytes, 0, bytes.Length);

            return hash;
        }

        public static string ToAddressSizeBase64String(string value)
        {
            return Utils.ToBase64String(ToAddressSizeArray(value));
        }

        public static byte[] ReadBytes(Stream stream)
        {
            byte[] buffer = new byte[4];
            
            stream.Read(buffer, 0, buffer.Length);

            int length = BitConverter.ToInt32(buffer, 0);

            byte[] result = new byte[length];

            stream.Read(result, 0, length);

            return result;
        }

        public static byte[] ReadBytes(byte[] data, int offset = 0)
        {
            if(offset >= data.Length)
                return new byte[0];
                 
            byte[] buffer = new byte[4];

            int length = BitConverter.ToInt32(data, offset);

            return data.Skip(buffer.Length + offset).Take(length).ToArray();
        }

        static Base64Encoder f = new Base64Encoder('-', '_', true);

        public static string ToBase64String(byte[] term)
        {
            return term == null ? null : f.ToBase(term);
                //Base64ReplaceRegexSlash.Replace(Base64ReplaceRegexPlus.Replace(Convert.ToBase64String(term), "-"), "_");//.Replace('/', '_').Replace('+', '-').Replace('=','=');
        }
        
        public static string DisplayBytes(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount < 1)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return string.Format("{0:n1} {1}", (Math.Sign(byteCount) * num), suf[place]);
        }

        public static void AppendAllBytes(string filename, byte[] data)
        {
            using (var stream = new FileStream(filename, FileMode.Append))
            {
                stream.Write(data, 0, data.Length);
            }
        }

        public static int GetAvaiablePort()
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            int port = 0;

            while (port == 0)
            {
                port = Rand.Next(10000, 50000);

                if (tcpConnInfoArray.Any(x => x.Port == port))
                    port = 0;
            }

            return port;
        }

        public static bool IsDirectory(string path)
        {
            FileAttributes attr = File.GetAttributes(path);

            return (attr & FileAttributes.Directory) == FileAttributes.Directory;
        }

        
        public static byte[] AddressFromBase64String(string base64String)
        {
            if (base64String == null)
                return null;
                     
            if (base64String.Length != pParameters.base64AddressSize)
                return null;

            try
            {
                return Convert.FromBase64String(base64String.Replace('_', '/').Replace('-', '+').Replace('=','='));
            }
            catch { }

            return null;
        }

        public static string ToSimpleAddress(byte[] data)
        {
#if !SIMPLE
            return ToBase64String(data);
#endif
            return ToSimpleAddress(ToSimpleName(data));
        }

        public static string ToSimpleAddress(string simpleName)
        {
#if !SIMPLE
            return simpleName;
#endif


            if (SimpleAddress == null)
                SimpleAddress = System.IO.File.ReadLines("endereços.txt").ToArray();

            return (Array.IndexOf(SimpleAddress, simpleName) + 1).ToString("d3");
        }

        public static string ToSimpleName(byte[] data)
        {
#if !SIMPLE
            return string.Empty;           
#endif

            if (data == null)
                return string.Empty;

            if (string.IsNullOrEmpty(SimpleNames[0]))
            {
                for(var i = 0; i < 65536; i++)
                    SimpleNames[i] = i.ToString("d5");

            }
                //SimpleNames = System.IO.File.ReadLines("names.txt").ToArray();

            var hash = SHA.ComputeHash(data, 0, data.Length); // CRC.ComputeChecksum(data);

            var h = BitConverter.ToUInt16(hash, 0);

            return SimpleNames[h];
        }

        public static bool Roll(double probability)
        {
            int p = (int)Math.Round(probability * 100);

            return (Rand.Next(100) < p);
        }

        public static Stream ToStream(this Image image, ImageFormat formaw)
        {
            var stream = new MemoryStream();
            image.Save(stream, formaw);
            stream.Position = 0;
            return stream;
        }

        public static string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        public static string ToHumanEllapsedTime(DateTime time)
        {
            var span = DateTime.UtcNow.Subtract(time); 

            if (DateTime.Now.Subtract(span).Day == DateTime.Now.AddDays(-1).Day)
                return "Yesterday";

            else if (span.TotalDays >= 2)
                return string.Format("{0} days ago", (int)span.TotalDays);

            else if ((int)span.TotalHours == 1)
                return string.Format("One hour ago");

            else if (span.TotalHours >= 2)
                return string.Format("{0} hours ago", (int)span.TotalHours);

            else if (span.TotalMinutes >= 2)
                return string.Format("{0} minutes ago", (int)span.TotalMinutes);

            else
                return "Just now";
        }

        static int prints = 1;

        internal static string framePrint = string.Empty;

        public static void PrintSearchResult(byte[] search, MetaPacketType type, IEnumerable<Metapacket> metapackets)
        {
#if !PRINT
            return;
#endif
            if (framePrint == string.Empty)
                framePrint = Client.Print();
            var simple = int.Parse(Utils.ToSimpleAddress(search));

            if (true || simple > 350)
            {

                var tmp = framePrint + simple + " [shape=ellipse, color=\"#FF0000\", style=filled];\r\n";


                foreach (var m in metapackets)
                {
                    tmp += Utils.ToSimpleAddress(m.Address) + " [shape=ellipse, color=\"#FFFF00\", style=filled];\r\n";

                }

                Utils.Print(tmp);
            }
        }

        public static void Print(string value = null)
        {
#if PRINT

            GraphGeneration wrapper;

            Process process = null;

            var getStartProcessQuery = new GetStartProcessQuery();
            var getProcessStartInfoQuery = new GetProcessStartInfoQuery();
            var registerLayoutPluginCommand = new RegisterLayoutPluginCommand(getProcessStartInfoQuery, getStartProcessQuery);

            wrapper = new GraphGeneration(getStartProcessQuery,
                                  getProcessStartInfoQuery,
                                  registerLayoutPluginCommand);


            if (value == null)
                value = Client.Print();
            
            byte[] output = wrapper.GenerateGraph("digraph{" + value+ "}", Enums.GraphReturnType.Png);

            if (output != null && output.Length > 0)
            {
                var path = @"print\" +prints++.ToString() + ".png";

                File.WriteAllBytes(path, output);

                if (process != null)
                    process.Close();

                //process = System.Diagnostics.Process.Start("file:///" + path);
            }

#endif
        }
    }

    

    public enum Crc16Mode : ushort { Standard = 0xA001, CcittKermit = 0x8408 }

    public class Crc16
    {
        readonly ushort[] table = new ushort[256];

        public ushort ComputeChecksum(params byte[] bytes)
        {
            ushort crc = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            return crc;
        }

        public byte[] ComputeChecksumBytes(params byte[] bytes)
        {
            ushort crc = ComputeChecksum(bytes);
            return BitConverter.GetBytes(crc);
        }

        public Crc16(Crc16Mode mode)
        {
            ushort polynomial = (ushort)mode;
            ushort value;
            ushort temp;
            for (ushort i = 0; i < table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
        }
    }

}
