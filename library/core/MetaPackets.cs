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
            internal MetaPacketType Type = MetaPacketType.Link;

            internal TimeCounter LocalAddressDistance = new TimeCounter(10, 100);

            internal TimeCounter LastAccess = new TimeCounter(10, 100);

            internal int Count = 0;

            internal Queue<byte[]> Queue = new Queue<byte[]>();

            internal ManualResetEvent Event = new ManualResetEvent(true);

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


        #region Thread Refresh

        internal static void Start(MetaPacketType type)
        {
            Load(type);

            Thread thread = null;

            thread = new Thread(Refresh);

            thread.Start(type == MetaPacketType.Link ? Links : Hashs);
        }

        internal static void Stop(MetaPacketType type)
        {
            if (type == MetaPacketType.Link)
                Links.Event.Set();
            else
                Hashs.Event.Set();

            Save(type);
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
                    c.Event.Reset();

                    c.Event.WaitOne();
                }


                if (Client.Stop)
                    break;

                if (!c.Queue.Any())
                    FillQueue(c.Items, c.Queue);

                if (!c.Queue.Any())
                {
                    c.Event.Reset();

                    c.Event.WaitOne();
                }

                if (Client.Stop)
                    break;

                byte[] address = c.Queue.Dequeue();

                Client.Stats.belowMinSentEvent.WaitOne();  //todo: or max confomr % de uso, ver outro uso

                if (Client.Stop)
                    break;

                var total = c.Count;

                //Probability by total packets
                var pT = (total - pParameters.MetaPacketsMaxItems) / total;

                var avgDistance = c.LocalAddressDistance.Average;

                //Probability by address distance to Local address
                var pL = Math.Log(Addresses.EuclideanDistance(Client.LocalPeer.Address, address), avgDistance);

                var toSincronize = new List<Metapacket>();

                var toRemove = new List<Metapacket>();

                foreach (var m in c.Items[address])
                {
                    var lastAccess = DateTime.Now.Subtract(m.LastAccess).TotalMinutes;

                    var avgLastAccess = c.LastAccess.Average;

                    //probability by last access
                    var pA = Math.Log(lastAccess, avgLastAccess);

                    if (lastAccess < avgLastAccess || pL == 0 || pA == 0 || Utils.Roll(1 / pL * 1 / pA))
                    {
                        toSincronize.Add(m);
                    }

                    if (pT > 0 && Utils.Roll(pT * pL * pA))
                    {

                        toRemove.Add(m);
                    }
                }

                if (toSincronize.Any())
                    Sincronize(c, toSincronize);

                if (toRemove.Any())
                    Remove(c, toRemove);

            }
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
                    command: stats.Type == MetaPacketType.Hash ? RequestCommand.Hashs : RequestCommand.Metapackets,
                    address: toSincronize[0].Address, 
                    originalPeer: Client.LocalPeer, 
                    senderPeer: Client.LocalPeer,
                    destinationPeer: peer,
                    data: data.ToArray());

                request.Enqueue();
            }
        }

        static void FillQueue(Dictionary<byte[], List<Metapacket>> list, Queue<byte[]> queue)
        {
            lock (list)
                queue = new Queue<byte[]>(list.OrderBy(x => Utils.Rand.Next()).Take(pParameters.MetaPacketsMaintenanceQueueSize).Select(x => x.Key));
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

            foreach(var m in metapackets)
            {
                m.Type = type;

                Add(m);
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

            foreach(var m in metapackets)
            {
                ToBytes(m, true).CopyTo(result, offset);

                offset += metapacket_size;
            }

            return result;
        }

        internal static byte[] ToBytes(Metapacket metapacket, bool includeAddress)
        {
            //Log.Write("---to bytes ---- ");

            //Log.Write(metapacket.ToString());

            return
                (includeAddress? metapacket.Address : new byte[0]).

                Concat(metapacket.TargetAddress).
                Concat(metapacket.LinkAddress).
                Concat(metapacket.Hash ?? Addresses.zero).
                Concat(BitConverter.GetBytes(metapacket.LastAccess.ToBinary())).
                Concat(BitConverter.GetBytes(metapacket.Creation.ToBinary())).
                    ToArray();
        }

        internal static Metapacket[] FromBytes(byte[] packet)
        {
            if (!packet.Any())
                return null;

            if (packet.Length % metapacket_size != 0)
                return null;

            var result = new Metapacket[packet.Length / metapacket_size];

            var offset = 0;

            for(var i = 0; i < result.Length; i ++)
            {
                result[i] = FromBytes(packet, offset);

                offset += metapacket_size;
            }

            return result;
        }

        static Metapacket FromBytes(byte[] packet, int offset, byte[] address = null)
        {
            //When loading from file the addres is on the file name
            //On receiving via p2p the address is the first data on the packet
            if (address == null)
            {
                address = packet.Skip(offset).Take(pParameters.addressSize).ToArray();

                offset += address.Length;
            }

            var targetAddress = packet.Skip(offset).Take(pParameters.addressSize).ToArray();

            offset += targetAddress.Length;

            var linkAddress = packet.Skip(offset).Take(pParameters.addressSize).ToArray();

            offset += linkAddress.Length;

            var hashAddress = packet.Skip(offset).Take(pParameters.addressSize).ToArray();

            offset += hashAddress.Length;

            if (Addresses.Equals(hashAddress, Addresses.zero, true))
                hashAddress = null;
            else
            {

            }

            Int64 lastAccess = BitConverter.ToInt64(packet, offset);

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


        internal static void Add(Metapacket metapacket)
        {
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
                            MostUsedItems.Add(new KeyValuePair<byte[], List<Metapacket>>(metapacket.TargetAddress,  itemsPerKey ));
                    }

                   
                }

                MostUsedRatio.Add(count);
            }
        }

        internal static IEnumerable<Metapacket> LocalSearch(byte[] address, MetaPacketType type)
        {
            if(Utils.ToSimpleAddress(address) == "001")
            {

            }

            var result = new Metapacket[0];

            if(type == MetaPacketType.Link)
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
