using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace library
{
    static class MetaPackets
    {
        static int metapacket_size = pParameters.addressSize * 4 + sizeof(Int64) * 2; //four address: address, link, target & hash + two dates: creation & last access

        internal class Stats
        {
            internal int totalAtFillQueue = 0;

            internal double probabilityByMaxMetapackets = 0;

            internal MetaPacketType Type = MetaPacketType.Link;

            internal TimeCounter LocalAddressDistance = new TimeCounter(10, 100);

            internal TimeCounter LastAccess = new TimeCounter(10, 100);

            internal int Count = 0;

            internal Queue<byte[]> Queue = new Queue<byte[]>();

            internal ManualResetEvent LocalPacketEvent = new ManualResetEvent(true);

            internal ManualResetEvent aboveMaxMetaPacketsEvent = new ManualResetEvent(false);

            internal Dictionary<byte[], List<Metapacket>> Items = new Dictionary<byte[], List<Metapacket>>(new ByteArrayComparer());

            internal Stats(MetaPacketType type)
            {
                Type = type;
            }
        }

        internal static Stats Links = new Stats(MetaPacketType.Link);

        internal static Stats Hashs = new Stats(MetaPacketType.Hash);

        internal static Cache<KeyValuePair<byte[], List<Metapacket>>> MostUsedItems = new Cache<KeyValuePair<byte[], List<Metapacket>>>(10 * 60 * 1000);

        internal static Counter MostUsedRatio = new Counter(0);

        internal static string ToString()
        {
            var result = string.Empty;

            foreach(var r in Hashs.Items)
            {
                foreach (var rr in r.Value)
                    result += rr.ToString() + "\r\n";
            }

            foreach (var r in Links.Items)
            {
                foreach (var rr in r.Value)
                    result += rr.ToString() + "\r\n";
            }

            return result;

        }


        #region Thread Refresh

        internal static void Start(MetaPacketType type)
        {
            Load(type);

            Thread thread = null;

            thread = new Thread(Refresh);

            thread.Name = type.ToString();

            thread.Start(type == MetaPacketType.Link ? Links : Hashs);
        }

        internal static void Stop(MetaPacketType type)
        {
            if (type == MetaPacketType.Link)
            {
                Links.LocalPacketEvent.Set();

                Links.aboveMaxMetaPacketsEvent.Set();
            }
            else
            {
                Hashs.LocalPacketEvent.Set();

                Hashs.aboveMaxMetaPacketsEvent.Set();
            }

            Save(type);
        }


        public static int GetThreads(bool w = true)
        {
            var iA = 0;
            var iW = 0;

            ThreadPool.GetAvailableThreads(out iW, out iA);

            Log.Write(iW.ToString() + "\t" + iA.ToString());

            return w ? iW : iA;
        }

        static void Refresh(object o)
        {
            var c = (Stats)o;

            while (!Client.Stop)
            {
                bool any;

                lock (c.Items)
                    any = c.Items.Any();

                if (!any)
                {
                    c.LocalPacketEvent.Reset();

                    c.LocalPacketEvent.WaitOne();
                }


                if (Client.Stop)
                    break;

                if (!c.Queue.Any())
                    FillQueue(ref c);

                if (!c.Queue.Any())
                {
                    c.LocalPacketEvent.Reset();

                    c.LocalPacketEvent.WaitOne();
                }

                // GetThreads();

                if (Client.Stop)
                    break;

                if (!c.Queue.Any()) 
                    continue;

                byte[] address = c.Queue.Dequeue();

                var probabilityByAddressDistance = Math.Log(Addresses.EuclideanDistance(Client.LocalPeer.Address, address) * 100, Peers.TopAverageDistance * 100) - 1;

                if (double.IsNaN(probabilityByAddressDistance))
                    probabilityByAddressDistance = 1;

                var toRemove = new List<Metapacket>();

                foreach (var m in c.Items[address])
                {
                    var lastAccess = DateTime.Now.Subtract(m.LastAccess).TotalMinutes;

                    var maxLastAccess = c.LastAccess.Average * 2;

                    var metapacketLastAccessPercent = lastAccess / maxLastAccess;

                    var averagePercent = c.LastAccess.Average / maxLastAccess;

                    var probabilityByLastAccess = Math.Log(100 * metapacketLastAccessPercent, 100 * averagePercent) - 1;

                    if (double.IsNaN(probabilityByLastAccess))
                        probabilityByLastAccess = 1;

                    if (c.probabilityByMaxMetapackets > 0 && Utils.Roll((c.probabilityByMaxMetapackets + probabilityByAddressDistance + probabilityByLastAccess) / 3d))
                    {
                        toRemove.Add(m);

                        c.totalAtFillQueue--;

                        c.probabilityByMaxMetapackets = ((double)c.totalAtFillQueue - pParameters.MetaPacketsMaxItems) / c.totalAtFillQueue;
                    }


                }

                if (toRemove.Any())
                    Remove(c, toRemove);
            }
        }

        static void FillQueue(ref Stats c)
        {
            c.totalAtFillQueue = c.Items.Count;

            c.probabilityByMaxMetapackets = (c.totalAtFillQueue - pParameters.MetaPacketsMaxItems) / (double)c.totalAtFillQueue;

            if (c.probabilityByMaxMetapackets < (pParameters.MinMetapacketsMaintenanceQueueSize / c.totalAtFillQueue))
            {
                c.aboveMaxMetaPacketsEvent.Reset();

                c.aboveMaxMetaPacketsEvent.WaitOne();
            }

            lock (c.Items)
                c.Queue = new Queue<byte[]>(c.Items.OrderBy(x => Utils.Rand.Next()).Take(pParameters.MetaPacketsMaintenanceQueueSize).Select(x => x.Key));
        }

        private static void Remove(Stats stats, List<Metapacket> toRemove)
        {
            lock (stats.Items)
            {
                var address = toRemove[0].TargetAddress;

                foreach (var m in toRemove)
                {
                    stats.Items[address].Remove(m);

                    stats.Count--;
                }

                if (!stats.Items[address].Any())
                    stats.Items.Remove(address);
            }
        }

        private static void Sincronize(Stats stats, List<Metapacket> toSincronize)
        {

            return;
            var data = new List<byte>();

            data.AddRange(toSincronize.First().TargetAddress);

            foreach (var b in toSincronize)
                data.AddRange(ToBytes(b, true));

            var peer = Peers.GetPeer(
               closestToAddress: toSincronize[0].Address,
               excludeOriginAddress: Client.LocalPeer.Address);

            if (peer != null)
            {
                p2pRequest request = new p2pRequest(
                    command:         stats.Type == MetaPacketType.Hash ? RequestCommand.Hashs : RequestCommand.Metapackets,
                    address:         toSincronize[0].Address,
                    originalPeer:    Client.LocalPeer,
                    senderPeer:      Client.LocalPeer,
                    destinationPeer: peer,
                    data:            data.ToArray());

                request.Enqueue();
            }

        }

     

        internal static void ExpandSearch()
        {
            var address = Utils.ToAddressSizeArray("cet"); //Client.LocalPeer.Address

            var toExpand = Hashs.Items.OrderBy(x => Addresses.EuclideanDistance(x.Key, address)).First();

            var p2pContext = new p2pContext();

            var contextId = Utils.GetAddress();

            var r = new SearchResult(contextId, Utils.ToBase64String(toExpand.Value.First().TargetAddress), RenderMode.Nav, p2pContext, null, true);

            var result = string.Empty;

            var temp_result = "[]";

            while (result == string.Empty && temp_result == "[]")
            {
                temp_result = r.GetResultsResults(p2pContext, address);// toExpand.Value.First().TargetAddress);

                if (temp_result != "[]")
                {
                    result = temp_result;
                    temp_result = "[]";
                }

                break;
            }

            DelayedWrite.Add(Path.Combine(pParameters.json, Utils.ToBase64String(address)), System.Text.Encoding.UTF8.GetBytes(result));
        }

        #endregion



        #region Serialization

        internal static void Load(MetaPacketType type)
        {
            string filename = type.ToString() + ".bin";

            if (!File.Exists(filename))
            {
                if (File.Exists(filename + ".new"))
                    File.Move(filename + ".new", filename);
                else if (File.Exists(filename + ".old"))
                    File.Move(filename + ".old", filename);
                else
                    return;
            }

            var buffer = File.ReadAllBytes(filename);

            var metapackets = FromBytes(buffer);

            foreach (var m in metapackets)
            {
                m.Type = type;

                Add(m, Client.LocalPeer);
            }

            /*
            var offset = 0;

            while (offset < buffer.Length)
            {
                var address = buffer.Skip(offset).Take(pParameters.addressSize).ToArray();

                offset += pParameters.addressSize;

                var packet = Packets.Get(address);

                if (packet != null)
                {
                    var metapacket = FromBytes(packet, 0, address);

                    if (metapacket == null)
                        continue;

                    metapacket.Type = type;

                    //metapacket.Address = address;

                    Add(metapacket);
                }
            }
            */

            //byte[] buffer = File.ReadAllBytes(Parameters.localPostsFile);

            //int reg_size = Peers.reg_size;

            //ReturnTime.Add(BitConverter.ToDouble(buffer, 0));

            //LocalAddressDistance.Add(BitConverter.ToDouble(buffer, 8));

            //buffer = buffer.Skip(16).ToArray();

            //Peer[] pp = new Peer[buffer.Length / reg_size];

            //for (int i = 0; i < buffer.Length / reg_size; i++)
            //{
            //    var peer = FromBytes(buffer.Skip(i * (reg_size)).Take(reg_size).ToArray());

            //    peers.Add(peer);
            //}


        }

        internal static void Save(MetaPacketType type)
        {
            //return;
            List<byte> data = new List<byte>();

            Stats stats = type == MetaPacketType.Link ? Links : Hashs;


            //data.AddRange(BitConverter.GetBytes(stats.LocalAddressDistance.Average));

            //data.AddRange(BitConverter.GetBytes(stats.LastAccess.Average));

            //data.AddRange(BitConverter.GetBytes(stats.Count));

            var list = stats.Items;

            lock (list)
            {
                foreach (var a in list.Keys)
                {
                    //data.AddRange(a);

                    //data.AddRange(BitConverter.GetBytes(list[a].Count()));

                    foreach (var b in list[a])
                    {
                        data.AddRange(ToBytes(b, true));
                    }
                }

            }

            string filename = type.ToString() + ".bin";

            string newFilename = filename + ".new";

            File.WriteAllBytes(newFilename, data.ToArray());

            if (File.Exists(filename + ".old"))
                File.Delete(filename + ".old");

            if (File.Exists(filename))
                File.Move(filename, filename + ".old");

            File.Move(newFilename, filename);
        }



        internal static byte[] ToBytes(IEnumerable<Metapacket> metapackets)
        {



            int offset = 0;

            var result = new byte[metapacket_size * metapackets.Count()];

            foreach (var m in metapackets)
            {
                ToBytes(m, true).CopyTo(result, offset);

                offset += metapacket_size;
            }

            Log.Add(Log.LogTypes.P2p, Log.LogOperations.Serialize, new { Packet = result.Length, Metapackets = metapackets.Select(x => x.ToString()).Aggregate<string>((a, b) => a + ";  " + b) });

            return result;
        }

        internal static byte[] ToBytes(Metapacket metapacket, bool includeAddress)
        {
            //Log.Write("---to bytes ---- ");

            //Log.Write(metapacket.ToString());

            //var result_length = ((includeAddress ? 4 : 3) * pParameters.addressSize) + (2 * 64); //(4 address) + (2 dates)

            //var result = new byte[result_length];

            //var offset = 0;

            //if (includeAddress)
            //{
            //    Buffer.BlockCopy(metapacket.Address, 0, result, offset, pParameters.addressSize);

            //    offset += pParameters.addressSize;
            //}

            //Buffer.BlockCopy(metapacket.TargetAddress, 0, result, offset, pParameters.addressSize);

            //offset += pParameters.addressSize;

            //Buffer.BlockCopy(metapacket.LinkAddress, 0, result, offset, pParameters.addressSize);

            //offset += pParameters.addressSize;

            //if(null != metapacket.Hash)
            //    Buffer.BlockCopy(metapacket.Hash, 0, result, offset, pParameters.addressSize);

            //offset += pParameters.addressSize;


            //Buffer.BlockCopy(BitConverter.GetBytes(metapacket.LastAccess.ToBinary()), 0, result, offset, pParameters.addressSize);

            //offset += pParameters.addressSize;

            //Buffer.BlockCopy(BitConverter.GetBytes(metapacket.Creation.ToBinary()), 0, result, offset, pParameters.addressSize);

            var r2 =

                (includeAddress ? metapacket.Address : new byte[0]).

                Concat(metapacket.TargetAddress).
                Concat(metapacket.LinkAddress).
                Concat(metapacket.Hash ?? Addresses.zero).
                Concat(BitConverter.GetBytes(metapacket.LastAccess.ToBinary())).
                Concat(BitConverter.GetBytes(metapacket.Creation.ToBinary())).
                    ToArray();


            //if(!Addresses.Equals(r2, result, true))
            //{

            //}
            //else
            //{

            //}

            return r2;
        }

        internal static Metapacket[] FromBytes(byte[] packet)
        {
            if (!packet.Any())
                return null;

           // if (packet.Length % metapacket_size != 0)
           //     packet = packet.Skip(pParameters.addressSize).ToArray();

            if (packet.Length % metapacket_size != 0)
                return null;


            var result = new Metapacket[packet.Length / metapacket_size];

            var offset = 0;

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = FromBytes(packet, offset);

                offset += metapacket_size;
            }

            Log.Add(Log.LogTypes.P2p, Log.LogOperations.Deserialize, new { Packet = packet.Length, Metapackets = result.Select(x => x.ToString()).Aggregate<string>((a,b)=>a + ";  " +b) });

            return result;
        }

        static Metapacket FromBytes(byte[] packet, int offset)
        {
            var address = new byte[pParameters.addressSize];

            Buffer.BlockCopy(packet, offset, address, 0, pParameters.addressSize);

            offset += pParameters.addressSize;

            var targetAddress = new byte[pParameters.addressSize];
            var linkAddress = new byte[pParameters.addressSize];
            var hashAddress = new byte[pParameters.addressSize];

            Buffer.BlockCopy(packet, offset, targetAddress, 0, pParameters.addressSize);

            offset += pParameters.addressSize;

            Buffer.BlockCopy(packet, offset, linkAddress, 0, pParameters.addressSize);

            offset += pParameters.addressSize;

            Buffer.BlockCopy(packet, offset, hashAddress, 0, pParameters.addressSize);

            offset += pParameters.addressSize;

            if (Addresses.Equals(hashAddress, Addresses.zero, true))
                hashAddress = null;



            var lastAccess = BitConverter.ToInt64(packet, offset);

            offset += sizeof(Int64);

            var lcreation = BitConverter.ToInt64(packet, offset);

            var creation = DateTime.FromBinary(lcreation);

            Metapacket result = new Metapacket(creation, targetAddress, linkAddress, hashAddress, MetaPacketType.Link, address);

            result.LastAccess = DateTime.FromBinary(lastAccess);

            //Log.Write("---from bytes---");

            //Log.Write(result.ToString());

            return result;
        }

        #endregion

         
        internal static void Add(Metapacket metapacket, Peer peer)
        {
            if (peer.Equals(Client.LocalPeer))
            {

                var destinationPeer = Peers.GetPeer(
                                   closestToAddress: metapacket.Address,
                                   excludeOriginAddress: Client.LocalPeer.Address);

                p2pRequest request = new p2pRequest(
                    address: metapacket.Address,
                    command: RequestCommand.Metapackets,
                    originalPeer: Client.LocalPeer,
                    senderPeer: Client.LocalPeer,
                    destinationPeer: destinationPeer,
                    data: MetaPackets.ToBytes(new Metapacket[] { metapacket }));

                request.Enqueue();

                destinationPeer = Peers.GetPeer(
                                   closestToAddress: metapacket.TargetAddress,
                                   excludeOriginAddress: Client.LocalPeer.Address);

                request = new p2pRequest(
                    address: metapacket.Address,
                    command: RequestCommand.Metapackets,
                    originalPeer: Client.LocalPeer,
                    senderPeer: Client.LocalPeer,
                    destinationPeer: destinationPeer,
                    data: MetaPackets.ToBytes(new Metapacket[] { metapacket }));

                request.Enqueue();
            }
            else
            {
                //todo:
                //if (!VerifyIntegrity(address, data, peer))
                //{
                //    OnPacketValidatorError?.Invoke(address);

                //    return;
                //}
            }

            var mode = metapacket.Type == MetaPacketType.Hash ? Hashs : Links;

            var list = mode.Items;

            lock (list)
                if (list.ContainsKey(metapacket.TargetAddress))
                    list[metapacket.TargetAddress].Add(metapacket);
                else
                    list.Add(metapacket.TargetAddress, new List<Metapacket>(new Metapacket[] { metapacket }));

            if (mode == Links)
            {
                var itemsPerKey = list[metapacket.TargetAddress];

                var count = itemsPerKey.Count();

                if (count > MostUsedRatio.Average)
                {
                    lock (MostUsedItems)
                    {
                        var item = MostUsedItems.FirstOrDefault(x => Addresses.Equals(x.CachedValue.Key, metapacket.TargetAddress));

                        if (item != null)
                            item.Reset();
                        else
                            MostUsedItems.Add(new KeyValuePair<byte[], List<Metapacket>>(metapacket.TargetAddress, itemsPerKey));
                    }


                }

                MostUsedRatio.Add(count);

                Links.LocalPacketEvent.Set();
            }
            else
                Hashs.LocalPacketEvent.Set();


        }

        internal static IEnumerable<Metapacket> LocalSearch(byte[] address, MetaPacketType type)
        {
            
            var result = new Metapacket[0];

            if (type == MetaPacketType.Link)
            {
                IEnumerable<Metapacket> res = new List<Metapacket>();

                res = MostUsedItems.Where(x =>
                        Addresses.Equals(x.CachedValue.Key, address, true)).
                        Select(x => x.CachedValue.Value).
                        Aggregate(res, (x, y) => x.Union(y));

                if (res.Any())
                {
                    List<Metapacket> byAddress = new List<Metapacket>();

                    foreach (var k in MostUsedItems)
                    {
                        var l = k.CachedValue.Value;

                        foreach (var ç in l)
                        {
                            if (Addresses.Equals(ç.Address, address, true) || Addresses.Equals(ç.LinkAddress, address, true))
                                byAddress.Add(ç);
                        }
                    }

                    result = res.Concat(byAddress).ToArray();

                    // return res;
                }
            }

            var list = type == MetaPacketType.Link ? Links.Items : Hashs.Items;

            lock (list)
            {
                IEnumerable<Metapacket> res = new List<Metapacket>();

                res = list.Where(x =>
                    Addresses.Equals(x.Key, address, type == MetaPacketType.Hash)).
                    Select(x => x.Value).
                    Aggregate(res, (x, y) => x.Union(y));

                if (type == MetaPacketType.Link)
                {
                    List<Metapacket> byAddress = new List<Metapacket>();

                    foreach (var k in list.Keys)
                    {
                        var l = list[k];

                        foreach (var ç in l)
                        {
                            if (Addresses.Equals(ç.Address, address, type == MetaPacketType.Hash) || Addresses.Equals(ç.LinkAddress, address, type == MetaPacketType.Hash))
                                byAddress.Add(ç);
                        }
                    }

                    res = res.Concat(byAddress);
                }

                result = result.Concat(res).ToArray();

                return result;
            }

        }

        internal static string Print(Stats type)
        {
            string result = string.Empty;

            foreach (var key in type.Items.Keys)
            {
                foreach (var m in type.Items[key])
                    result += m.ToString() + Environment.NewLine;
            }

            return result;
        }

        internal static byte[] LocalizeAddress(byte[] bTerm)
        {
            lock (Links.Items)
            {
                var t = Links.Items.FirstOrDefault(x => Addresses.Equals(x.Key, bTerm, true));

                return t.Key;
            }
        }
    }

    public enum MetaPacketType
    {
        Hash,
        Link
    }

    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] left, byte[] right)
        {
            return Addresses.Equals(left, right);
        }

        public int GetHashCode(byte[] array)
        {
            return
               BitConverter.ToInt32(array, 0);
        }
    }

    public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        // You could make this a per-instance field with a constructor parameter
        private static readonly EqualityComparer<T> elementComparer
            = EqualityComparer<T>.Default;

        public bool Equals(T[] first, T[] second)
        {
            if (first == second)
                return true;

            if (first == null || second == null)
                return false;

            var max = first.Length;

            if (max != second.Length)
                return false;

            for (int i = 0; i < max; i++)
                if (!elementComparer.Equals(first[i], second[i]))
                    return false;

            second = first;

            return true;
        }

        public int GetHashCode(T[] array)
        {
            unchecked
            {
                if (array == null)
                {
                    return 0;
                }
                int hash = 17;
                for (var i = 0; i < 8; i++)
                {
                    hash = hash * 31 + elementComparer.GetHashCode(array[i]);
                }
                return hash;
            }
        }
    }

}
