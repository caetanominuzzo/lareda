using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace library
{

    public partial class Client : IDisposable
    {
        internal static int MaxDeepness = 10;

        public delegate void SearchReturnHandler(byte[] search, MetaPacketType type, IEnumerable<Metapacket> metapackets);

        public static event SearchReturnHandler OnSearchReturn;


        public delegate void FileDownloadHandler(byte[] address, string filename, string speficFilena = null);

        public static event FileDownloadHandler OnFileDownload;


        public delegate void FileUploadHandler(string filename, string base64Address);

        public static event FileUploadHandler OnFileUpload;



        internal static Peer LocalPeer;

        internal static int P2pPort;

        internal static byte[] P2pAddress;

        public static IDisposable Start(byte[] p2pAddress, int p2pPort)
        {
            Log.Clear();

            P2pAddress = p2pAddress;

            P2pPort = p2pPort;

            Network.Configure();

            LocalPeer = Peers.CreateLocalPeer();

            DelayedWrite.Start();

            p2pServer.Start();

            p2pRequest.Start();

            Peers.Start();

            Packets.Start();

            MetaPackets.Start(MetaPacketType.Link);

            MetaPackets.Start(MetaPacketType.Hash);

            p2pFile.Queue.Load();

            return new Client();
        }

        internal static bool Stop
        {
            get;
            private set;
        }

        public static void Close()
        {
            Stop = true;

            p2pServer.Stop();

            Packets.Stop();

            Peers.Stop();

            p2pRequest.Stop();

            //LocalIndex.Save();

            MetaPackets.Stop(MetaPacketType.Link);

            MetaPackets.Stop(MetaPacketType.Hash);

            DelayedWrite.Stop();
        }

        public static string GetWelcomeKey()
        {
            if (LocalPeer.EndPoint.Address.Equals(IPAddress.Any))
                return string.Empty;

            return Utils.ToBase64String(Addresses.ToBytes(LocalPeer.EndPoint));
        }

        public static void GetInstaller()
        {
            var isntaller = @"C:\Users\caetano\Documents\la-red-menos\installer_windows_desktop\Publish\la red.msi";

            var target =
             Path.Combine(
                 Path.GetDirectoryName(AppDomain.CurrentDomain.FriendlyName),
                 "Install " + Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName) + " por " + Environment.UserName + ".msi");

            File.Copy(isntaller, target, true);

            var tail = new List<byte>();

            var info = new FileInfo(AppDomain.CurrentDomain.FriendlyName);

            tail.AddRange(CreateTailItem((int)info.Length));

            var peers = Peers.GetPeers(count: 100);

            if (peers == null)
                peers = new Peer[0];

            peers = peers.Concat(new Peer[] { Client.LocalPeer }).ToArray();


            var data = new List<byte>();

            foreach (var p in peers)
            {
                if (p != null)
                    data.AddRange(Peers.ToBytes(p));
            }

            tail.AddRange(CreateTailItem(data.Count()));

            using (var f = new FileStream(target, FileMode.Append))
            {
                f.Write(data.ToArray(), 0, data.Count());

                //CreateTailAssembly(f, tail, "library.dll");

                //CreateTailAssembly(f, tail, "Newtonsoft.Json.dll");

                //CreateTailAssembly(f, tail, "taglib-sharp.dll");

                var tailTail = CreateTailItem(tail.Count());

                tail.AddRange(CreateTailItem(tail.Count() + tailTail.Count()));

                f.Write(tail.ToArray(), 0, tail.Count());
            }


        }

        internal static void CreateTailAssembly(FileStream target, List<byte> tail, string filename)
        {
            var info = new FileInfo(filename);

            tail.AddRange(CreateTailItem((int)info.Length));

            var buffer = new byte[10 * 1024];

            var read = 0;

            using (var f = info.OpenRead())
                while ((read = f.Read(buffer, 0, buffer.Length)) > 0)
                    target.Write(buffer, 0, read);

        }

        internal static byte[] CreateTailItem(int dataSize)
        {
            var length = BitConverter.GetBytes(dataSize);

            var hash = Utils.ComputeHash(length, 0, length.Count());

            return length.Concat(hash).ToArray();
        }

        public static bool AnyPeer()
        {
            return true;
            return Peers.AnyPeer();
        }

        public static void GetPeer(string base64EndPoint)
        {
            byte[] b = Convert.FromBase64String(base64EndPoint.Replace('_', '/').Replace('-', '+').Replace('=', '='));

            if (b == null)
                return;

            IPEndPoint endpoint = new IPEndPoint(
                new IPAddress(b.Take(pParameters.ipv4Addresssize).ToArray()),
                BitConverter.ToUInt16(b, pParameters.ipv4Addresssize));

            Peer peer = Peers.GetPeer(endpoint);

            if (peer == null)
                peer = Peers.CreatePeer(endpoint, null);

            Peers.AddPeer(peer);
        }

        internal static void Search(byte[] address, MetaPacketType type)
        {

            Log.Write("Pesquisando local: " + Utils.ToSimpleAddress(address) + " - " + type.ToString() + "  " + Utils.ToSimpleAddress(address));

            var metapackets = MetaPackets.LocalSearch(address, type);

            if (metapackets.Any())
                SearchReturn(address, type, metapackets);

            Log.Write(metapackets.Count() + " encontrado(s).", 1);

            Log.Write("Pesquisando remoto: " + Utils.ToSimpleAddress(address), 1);

            int requisitions = 0;

            byte[] byte_key = address;

            while (requisitions < pParameters.propagation)
            {
                var s = new p2pRequest(type == MetaPacketType.Hash ? RequestCommand.Hashs : RequestCommand.Links, null, Client.LocalPeer, Client.LocalPeer, Peers.GetPeer(address), address);

                var sent = s.Send();

                if (sent)
                    requisitions++;
                else
                    break;
            }

        }

        internal static void DownloadComplete(byte[] address, string filename, string speficFilena = null)
        {
            if (OnFileDownload != null)
                OnFileDownload(address, filename, speficFilena);
        }

        internal static void SearchReturn(byte[] search, MetaPacketType type, IEnumerable<Metapacket> metapackets)
        {
            if (OnSearchReturn != null)
                OnSearchReturn(search, type, metapackets);
        }

        internal static void FileUpload(string filename, string base64Address)
        {
            if (OnFileUpload != null)
                OnFileUpload(filename, base64Address);
        }

        public static byte[] Post(string title = null, byte[] conceptAddress = null, string target = null, string userAddressBase64 = null, string[] refs = null, string content = null)
        {

            if (title != null && title.Contains("AlbumArtSmall"))
            {

            }
            var linkAddress = Utils.AddressFromBase64String(title);

            if (linkAddress != null)
            {
                if (target != null)
                {
                    var t = Utils.AddressFromBase64String(target);

                    if (t != null)
                    {
                        return Metapacket.Create(t, linkAddress).Address;
                    }
                }

                return null;
            }

            if (conceptAddress == null)
            {
                conceptAddress = Utils.GetAddress();

                Metapacket.Create(conceptAddress, VirtualAttributes.CONCEITO);
            }


            if (refs != null)
                foreach (var p in refs)
                {

                    var title_index = Utils.ToAddressSizeArray(p);

                    var title_hashs = MetaPackets.LocalSearch(title_index, MetaPacketType.Hash);

                    byte[] concept_ref = null;

                    if (title_hashs.Any())
                    {
                        concept_ref = title_hashs.First().LinkAddress;
                    }
                    else
                        concept_ref = Post(p);

                    Metapacket.Create(
                       targetAddress: concept_ref,
                       linkAddress: conceptAddress);
                }



            if (title != null)
            {
                var index = Utils.ToAddressSizeArray(title);

                var indexConcept = Metapacket.Create(
                    targetAddress: index,
                    linkAddress: conceptAddress,
                    type: MetaPacketType.Hash);

                p2pFile.StreamUpload(conceptAddress, PacketTypes.Content, new MemoryStream(Encoding.Unicode.GetBytes(title)), null, VirtualAttributes.MIME_TYPE_TEXT_THUMB);
            }

            if (content != null)
            {
                p2pFile.StreamUpload(conceptAddress, PacketTypes.Content, new MemoryStream(Encoding.Unicode.GetBytes(content)), null, VirtualAttributes.MIME_TYPE_TEXT);
            }

            if (userAddressBase64 != null)
            {
                var userAddress = MetaPackets.LocalizeAddress(Utils.AddressFromBase64String(userAddressBase64));

                if (userAddress != null)
                {
                    var user = Metapacket.Create(
                       targetAddress: conceptAddress,
                       linkAddress: userAddress);

                    Metapacket.Create(
                        targetAddress: user.Address,
                        linkAddress: VirtualAttributes.AUTHOR);
                }
            }

            if (target != null)
            {
                var t = Utils.AddressFromBase64String(target);

                if (t != null)
                    Metapacket.Create(
                       targetAddress: conceptAddress,
                       linkAddress: t);
            }

            return conceptAddress;
        }

        public static void PostImage(byte[] concept, byte[] post)
        {

            p2pFile.StreamUpload(concept, PacketTypes.Content, new MemoryStream(post), null, MIME_TYPE: VirtualAttributes.MIME_TYPE_IMAGE_THUMB);

        }

        public static byte[] CreateUser(string name)
        {
            var address = Post(name);

            // Metapacket.Create(
            //     Metapacket.Create(address, VirtualAttributes.TEMPLATE_PESSOA).Address,
            //     VirtualAttributes.TEMPLATE);

            return address;
        }

        public static void Upload(string[] filenames, byte[] userAddress)
        {
            var conceptAddress = Utils.GetAddress();

            p2pFile.FileUpload(filenames, conceptAddress, userAddress, true);
        }

        public static byte[] GetLocal(byte[] address)
        {
            var result = Packets.Get(address);

            if (result != null && result[0] == (int)PacketTypes.Content)
                return result.Skip(pParameters.packetHeaderSize).ToArray();

            return null;
        }

        public static void Download(string base64Address, string filename, string specifItem = null)
        {
            if (Connected())
                p2pFile.Queue.Add(base64Address, filename, specifItem);
        }

        private static bool Connected()
        {
            return true;
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        static void PostArticle(string title, string content, string[] links)
        {

        }

        public static void BootStrap()
        {
            VirtualAttributes.BootStrap();

            //VirtualAttributes.CONCEITO = Utils.GetAddress();

            //VirtualAttributes.AUTHOR = Utils.GetAddress();

            //VirtualAttributes.MIME_TYPE_TEXT = Utils.GetAddress();

            //VirtualAttributes.MIME_TYPE_TEXT_THUMB = Utils.GetAddress();

            //VirtualAttributes.MIME_TYPE_IMAGE = Utils.GetAddress();

            //VirtualAttributes.MIME_TYPE_IMAGE_THUMB = Utils.GetAddress();

            //VirtualAttributes.MIME_TYPE_DOWNLOAD = Utils.GetAddress();

            //VirtualAttributes.MIME_TYPE_STREAM = Utils.GetAddress();

            //VirtualAttributes.MIME_TYPE_DIRECTORY = Utils.GetAddress();

            //VirtualAttributes.Artist = Utils.GetAddress();


            //Client.Post("BBB", null, Utils.ToBase64String(Client.Post("AAA")));

            //return;

            var dir = @"C:\Users\caetano\Documents\Visual Studio 2013\Projects\Wikpedia\Wikpedia\bin\Debug\txt\";

            //var files = File.ReadAllLines(@"C:\Users\caetano\Documents\Visual Studio 2013\Projects\Wikpedia\Wikpedia\bin\Debug\graph.txt");

            //  files = new string[] { "/wiki/Mathematics;438;", "/wiki/Ancient_greek;438;", "/wiki/Quantity;438;", "/wiki/Number;438;", };

            //var count = 0;

            //foreach (var file in files)
            //{
            //    if (file.Contains("Real"))
            //    {

            //    }


            //    count++;

            //    if (count > -100)
            //        return;

            //    var items = file.Split(';');

            //    var url = items[0].Split('/');


            //    var filename = Path.Combine(dir, url[2].Replace('_', ' ') + ".txt");

            //    var name = Path.GetFileNameWithoutExtension(filename);

            //    if (!File.Exists(filename))
            //        continue;

            //    var lines = File.ReadAllLines(filename);

            //    var links = new List<string>();

            //    var paragraphs = new List<string>();

            //    foreach (var line in lines)
            //    {
            //        if (line.StartsWith("/wiki/"))
            //            links.Add(GetTitle(line));
            //        else
            //            paragraphs.Add(line);

            //        //  if (links.Any() && paragraphs.Any())
            //        //    break;
            //    }

            //    Client.Post(name, null, null, null, links.Take(2).ToArray(), string.Join(Environment.NewLine, paragraphs));

            //    // break;
            //}

        }

        static string GetTitle(string url)
        {
            var s = url.Split('/');

            return s[s.Length - 1].Replace('_', ' ');
        }

        static Metapacket BootStrapPost(string post, byte[] parent = null)
        {
            //1
            var concept = Utils.GetAddress();

            //2
            //Indice/Conceito HASH
            var indexConcept = Metapacket.Create(
                targetAddress: Utils.ToAddressSizeArray(post),
                linkAddress: concept,
                type: MetaPacketType.Hash);

            var contentAddress = Utils.GetAddress();

            //3
            Packets.Add(contentAddress, Encoding.Unicode.GetBytes(post), Client.LocalPeer);

            //4
            //Conceito/Conteudo - LINK
            var result = Metapacket.Create(
                targetAddress: concept,
                linkAddress: contentAddress,
                hashContent: Utils.ToAddressSizeArray(post));

            return result;
        }

        public static string Print()
        {
            //return CurrentSearch.RootResults.Print();

            var r = string.Empty;

            var colors = new string[] { "black", "red", "blue", "green", "yellow", "orange", "brown", "cyan", "orange", "blue", "red" };

            var i = 0;

            var cluster = 0;


            //  r += ranksame;

            foreach (var x in MetaPackets.Links.Items)
            {
                foreach (var y in x.Value)
                {
                    var simpleAddress = int.Parse(Utils.ToSimpleAddress(y.LinkAddress));

                    var addresss = int.Parse(Utils.ToSimpleAddress(y.Address));

                     if (addresss < 351)
                       continue;

                    var s = Utils.ToSimpleAddress(y.TargetAddress) + "->" + Utils.ToSimpleAddress(y.Address);


                    var tAddress = int.Parse(Utils.ToSimpleAddress(y.Address));

                    //if ((tAddress < 350))// || tAddress > 650))
                    //   continue;

                    if (simpleAddress > 0)
                        s += "->" + Utils.ToSimpleAddress(y.LinkAddress);

                    s += ";" + Environment.NewLine;



                    int linksCount = y.LinkAddress.Length / pParameters.addressSize;

                    if (simpleAddress <= VirtualAttributes.Count && simpleAddress > 0)
                    {
                        s = "\"" + Utils.ToSimpleAddress(y.Address) + "\" -> \"" + Utils.ToSimpleAddress(y.Address) + "-" + VirtualAttributes.PropertyIndex(int.Parse(Utils.ToSimpleAddress(y.LinkAddress))) + "\" [color=red]; \n";

                        s += Utils.ToSimpleAddress(y.TargetAddress) + "-> \"" + Utils.ToSimpleAddress(y.Address) + "\";" + Environment.NewLine;
                    }
                    else if (simpleAddress == 0)
                    {
                        var subgraph = "subgraph cluster" + cluster.ToString() + " { " + Environment.NewLine;

                        var first = string.Empty;

                        for (var j = 0; j < linksCount; j++)
                        {
                            if (first == string.Empty)
                                first = Utils.ToSimpleAddress(y.LinkAddress.
                                    Skip(j * pParameters.addressSize).
                                    Take(pParameters.addressSize).ToArray());

                            subgraph += Utils.ToSimpleAddress(y.LinkAddress.
                                Skip(j * pParameters.addressSize).
                                Take(pParameters.addressSize).ToArray()) + ";" + Environment.NewLine;
                        }

                        subgraph += "}" + Environment.NewLine;

                        r += subgraph + Utils.ToSimpleAddress(y.Address) + " -> " + first + " [lhead=cluster" + cluster++ + "];" + Environment.NewLine;

                    }

                    r += s;

                    //r += Utils.ToSimpleAddress(y.LinkAddress) + "->" + Utils.ToSimpleAddress(y.Address) + "->" + Utils.ToSimpleAddress(y.TargetAddress) + ";" + Environment.NewLine;

                    if (y.Hash != null)
                    {
                        var p = Packets.Get(y.LinkAddress);

                        var packet = "null";

                        if (p != null)
                        {
                            packet = "PACOTE_" + ((p.Length - 21) / 2).ToString();

                            if (p.Length < 100)
                                packet = "\"" + Encoding.Unicode.GetString(p.Skip(21).ToArray()) + "\"";
                        }


                        r += Utils.ToSimpleAddress(y.LinkAddress) + "->" + packet + ";" + Environment.NewLine;

                    }
                }
            }



            return r;
        }

        public static string GetPeers()
        {
            var peers = Peers.GetPeers();

            var sb = new StringBuilder();

            foreach (var p in peers)
            {
                sb.AppendLine(p.Serialize());
            }

            return sb.ToString();
        }

    }

    //public static class ArrayExtensions
    //{
    //    public static string Extend<T>(this T[] originalArray)
    //    {
    //        var simpleName = Utils.ToSimpleName((byte[])originalArray);

    //        var simpleAddress = Utils.ToSimpleAddress(simpleName);

    //        return "[" + simpleAddress + "] ";
    //    }
    //}
}
