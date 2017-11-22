using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static library.Query;

namespace library
{

    public partial class Client : IDisposable
    {
        internal static int MaxDeepness = 10;

        public delegate void SearchReturnHandler(byte[] search, MetaPacketType type, IEnumerable<Metapacket> metapackets);

        public static event SearchReturnHandler OnSearchReturn;


        public delegate void FileDownloadHandler(byte[] address, string filename, string speficFilena, int[] arrives, int[] cursors);

        public static event FileDownloadHandler OnFileDownload;





        public delegate void FileUploadHandler(string filename, string base64Address);

        public static event FileUploadHandler OnFileUpload;



        internal static Peer LocalPeer;

        internal static IPEndPoint P2pEndpoint;

        internal static byte[] P2pAddress;

        public static void StartThreads()
        {
            MetaPackets.Start(MetaPacketType.Link);

            MetaPackets.Start(MetaPacketType.Hash);
        }

        public static void ExpandSearch()
        {
            MetaPackets.ExpandSearch();
        }

        private static int distance(byte[] b1, byte[] b2)
        {
            int distance;
            distance = (b1[0] ^ b2[0]) & 0xFF;
            distance = ((b1[1] ^ b2[1]) & 0xFF) | (distance << 8);
            distance = ((b1[2] ^ b2[2]) & 0xFF) | (distance << 8);
            return ((b1[3] ^ b2[3]) & 0xFF) | (distance << 8);
        }

        public static IDisposable Start(byte[] p2pAddress, IPEndPoint p2pEndpoint)
        {
            for (var i = 0; i < 1; i++)
            {
                var A = Utils.GetAddress();

                var B = Utils.GetAddress();

                var C = Utils.GetAddress();

                var AB = Addresses.EuclideanDistance(A, B);

                var AC = Addresses.EuclideanDistance(A, C);

                var BC = Addresses.EuclideanDistance(B, C);


                //B = A.ToArray();

                var ab = Addresses.KadDistance(A, B);

                var ac = Addresses.KadDistance(A, C);

                var bc = Addresses.KadDistance(B, C);


                if (ab + ac < bc ||
                    ab + bc < ac ||
                    ac + bc < ab)
                {

                }

                if (AB +AC < BC ||
                    AB + BC < AC ||
                    AC + BC < AB)
                {

                }
            }



            Log.Clear();

            P2pAddress = p2pAddress;

            P2pEndpoint = p2pEndpoint;

            Network.Configure();

            LocalPeer = Peers.CreateLocalPeer(p2pEndpoint);

            DelayedWrite.Start();

            p2pServer.Start();

            p2pRequest.Start();

            Peers.Start();

            Packets.Start();

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
            //if (VirtualAttributes.IsVirtualAttribute(address))
            //{
            //    if(!Addresses.Equals(VirtualAttributes.ROOT_STREAM, address) &&
            //        !Addresses.Equals(VirtualAttributes.ROOT_POST, address) &&
            //        !Addresses.Equals(VirtualAttributes.ROOT_SEQUENCE, address))
            //        return;
            //}

            Log.Add(Log.LogTypes.Search, Log.LogOperations.Start, new { address = Utils.ToSimpleAddress(address) });

            var metapackets = MetaPackets.LocalSearch(address, type);

            if (metapackets.Any())
                SearchReturn(address, type, metapackets);

            int requisitions = 0;

            byte[] byte_key = address;

            while (requisitions < pParameters.propagation)
            {
                var s = new p2pRequest(type == MetaPacketType.Hash ? RequestCommand.Hashs : RequestCommand.Metapackets, null, Client.LocalPeer, Client.LocalPeer, Peers.GetPeer(address), address);

                var sent = s.Send();

                if (sent)
                    requisitions++;
                else
                    break;
            }

        }

        internal static void DownloadComplete(byte[] address, string filename, string speficFilena, int[] arrives, int[] cursors)
        {
            OnFileDownload?.Invoke(address, filename, speficFilena, arrives, cursors);
        }

        internal static void SearchReturn(byte[] search, MetaPacketType type, IEnumerable<Metapacket> metapackets)
        {
            OnSearchReturn?.Invoke(search, type, metapackets);
        }

        internal static void FileUpload(string filename, string base64Address)
        {
            OnFileUpload?.Invoke(filename, base64Address);
        }





        public static byte[] Post(string title = null, byte[] parentConceptAddress = null, string target = null, string userAddressBase64 = null, string[] refs = null, string content = null)
        {
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

            var conceptAddress = parentConceptAddress;// Utils.GetAddress();

            if (parentConceptAddress == null)
            {
                conceptAddress = Utils.GetAddress();

                Metapacket.Create(conceptAddress, VirtualAttributes.CONCEITO);

                var m = Metapacket.Create(conceptAddress, VirtualAttributes.ROOT_STREAM);

                Metapacket.Create(m.Address, VirtualAttributes.ROOT_TYPE);

                //if (parentConceptAddress != null)
                //    Metapacket.Create(conceptAddress, parentConceptAddress);
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

                p2pFile.StreamUpload(conceptAddress, PacketTypes.Content, new MemoryStream(Encoding.UTF8.GetBytes(title)), null, VirtualAttributes.MIME_TYPE_TEXT_THUMB);
            }

            if (content != null)
            {
                p2pFile.StreamUpload(conceptAddress, PacketTypes.Content, new MemoryStream(Encoding.UTF8.GetBytes(content)), null, VirtualAttributes.MIME_TYPE_TEXT);
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

            var root = Directory.Exists(filenames[0]) ? filenames[0] : Path.GetDirectoryName(filenames[0]);

            p2pFile.FileUpload(root, filenames, conceptAddress, userAddress, true);
        }

        public static byte[] GetLocal(byte[] address, byte[] hash)
        {
            var result = Packets.Get(address);

            if (result != null && result[0] == (int)PacketTypes.Content)
            {
                var real_hash = Utils.ComputeHash(result, pParameters.packetHeaderSize, result.Length - pParameters.packetHeaderSize);

                if (Addresses.Equals(real_hash, hash, true))
                    return result.Skip(pParameters.packetHeaderSize).ToArray();
            }

            return null;
        }

        public static void Download(string base64Address, string hash, p2pContext context, string filename, string specifItem = null)
        {
            if (Connected())
                p2pFile.Queue.Add(base64Address, hash, context, filename, specifItem);
        }

        public static NodeResult ExecuteQuery(string query, List<Metapacket> metapacket_list = null)
        {
            return Query.Execute(query, metapacket_list);
        }
        public static void Clear()
        {
            p2pFile.Queue.Clear();


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

        public static void BootStrap(object state)
        {
#if !BOOTSTRAP

            //   return;
#endif

            Log.Add(Log.LogTypes.All, Log.LogOperations.All, typeof(TagLib.ByteVector).FullName);

            VirtualAttributes.BootStrap();


            Client.Post("Ingles", VirtualAttributes.EN_US);

            Client.Post("Portugues", VirtualAttributes.PT_BR);



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

            //var dir = @"C:\Users\caetano\Documents\Visual Studio 2013\Projects\Wikpedia\Wikpedia\bin\Debug\txt\";

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

            var result = string.Empty;

            var declarations = new Dictionary<int, string>();

            var colors = new string[] { "black", "red", "blue", "green", "yellow", "orange", "brown", "cyan", "orange", "blue", "red" };

            var i = 0;

            var cluster = 0;


            //  r += ranksame;

            foreach (var x in MetaPackets.Links.Items)
            {
                foreach (var y in x.Value)
                {
                    var simpleAddress = int.Parse(y.SimpleLinkAddress);// int.Parse(Utils.ToSimpleAddress(y.LinkAddress));   

                    var addresss = int.Parse(y.SimpleAddress);

                    //if (addresss < 392)
                    //    continue;

                    var s = y.SimpleTargetAddress + "->" + y.SimpleAddress;


                    var tAddress = int.Parse(y.SimpleAddress);

                    //if ((tAddress <  350))// || tAddress > 650))
                    //   continue;

                    if (simpleAddress > 0)
                        s += "->" + y.SimpleLinkAddress;

                    s += ";" + Environment.NewLine;



                    int linksCount = y.LinkAddress.Length / pParameters.addressSize;

                    if (simpleAddress <= VirtualAttributes.Count && simpleAddress > 0)
                    {
                        if (VirtualAttributes.PropertyIndex(int.Parse(y.SimpleLinkAddress)) != "ROOT_TYPE" &&
                            VirtualAttributes.PropertyIndex(int.Parse(y.SimpleLinkAddress)) != "Culture")
                        {
                            var id = int.Parse(y.SimpleTargetAddress);

                            var subres = y.SimpleAddress + "-" + VirtualAttributes.PropertyIndex(int.Parse(y.SimpleLinkAddress)) + "<br/>";

                            if (declarations.ContainsKey(id))
                                declarations[id] += subres;
                            else
                                declarations.Add(id, subres);
                        }

                        s = "";
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

                        result += subgraph + Utils.ToSimpleAddress(y.Address) + " -> " + first + " [lhead=cluster" + cluster++ + "];" + Environment.NewLine;

                    }

                    if (s.Contains("545") || declarations.ContainsKey(545))
                    {

                    }

                    result += s;

                    //r += Utils.ToSimpleAddress(y.LinkAddress) + "->" + Utils.ToSimpleAddress(y.Address) + "->" + Utils.ToSimpleAddress(y.TargetAddress) + ";" + Environment.NewLine;

                    if (y.Hash != null)
                    {
                        var p = Packets.Get(y.LinkAddress);

                        var packet = "null";

                        if (p != null)
                        {


                            packet = "PACOTE_" + ((p.Length - 21) / 2).ToString();

                            if (p.Length < 85)
                                packet = Encoding.UTF8.GetString(p.Skip(pParameters.packetHeaderSize).ToArray());
                        }

                        var id = int.Parse(Utils.ToSimpleAddress(y.LinkAddress));

                        var subres = packet + "<br/>";

                        if (declarations.ContainsKey(id))
                            declarations[id] += subres;
                        else
                            declarations.Add(id, subres);


                        //result +=  + "->" + packet + ";" + Environment.NewLine;

                    }
                }
            }


            var s_declarations = declarations.Aggregate(new StringBuilder(),
              (sb, y) => sb.Append("\"" + y.Key + "\" [label=< <FONT POINT-SIZE=\"20\"><b>" + y.Key + "</b></FONT>" + Utils.ToBase64String(Utils.ToAddressSizeArray((y.Key - 1).ToString())).Substring(0, 5) + "<br /> " + y.Value.Replace("MIME_TYPE_", "") + ">];\n"),
              sb => sb.ToString());

            return s_declarations + result;
        }

        public static void CreateLotsOfPackets()
        {
            var d = new Dictionary<int, int>();

            for(var i = 0; i < 1000000; i++)
            {
                var dis = (int)Addresses.EuclideanDistance(Client.LocalPeer.Address, Utils.GetAddress());

                if (d.ContainsKey(dis))
                    d[dis] = d[dis] + 1;
                else
                    d[dis] = 1;

                //Packets.Add(Utils.GetAddress(), Utils.GetAddress(), Client.LocalPeer);
            }

            File.WriteAllLines("data2.txt", d.Select(x => x.Key.ToString() + ";" + x.Value.ToString()));


        }



        public static string listPackets()
        {
            var s = string.Empty;

            foreach (var x in MetaPackets.Links.Items)
            {
                foreach (var y in x.Value)
                {
                    var simpleAddress = int.Parse(y.SimpleLinkAddress);// int.Parse(Utils.ToSimpleAddress(y.LinkAddress));   

                    var addresss = int.Parse(y.SimpleAddress);

                    //if (addresss < 392)
                    //    continue;

                    s += y.SimpleTargetAddress + "->" + y.SimpleAddress;


                    var tAddress = int.Parse(y.SimpleAddress);

                    //if ((tAddress <  350))// || tAddress > 650))
                    //   continue;

                    if (simpleAddress > 0)
                        s += "->" + y.SimpleLinkAddress;

                    s += ";" + Environment.NewLine;
                }
            }

            return s;
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

