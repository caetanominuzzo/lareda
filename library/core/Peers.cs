using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace library
{
    static class Peers
    {
        static Dictionary<double, Peer> peers = new Dictionary<double, Peer>();

        static Queue<Peer> queue = new Queue<Peer>();

        internal static TimeCounter LocalAddressDistance = new TimeCounter(10, 10);

        internal static TimeCounter Latency = new TimeCounter(10, 10);

        internal static TimeCounter LastAccess = new TimeCounter(10, 10);

        internal static ManualResetEvent localPeerEvent = new ManualResetEvent(true);

        internal static ManualResetEvent aboveMaxPeersEvent = new ManualResetEvent(false);

        static double probabilityByMaxPeers = 0;

        internal static double TopAverageDistance = 0;

        static int totalAtFillQueue = 0;

        #region Thread Refresh

        internal static void Start()
        {
            Load();
            return;
            Thread thread = new Thread(Refresh);

            thread.Start();
        }

        internal static void Stop()
        {
            aboveMaxPeersEvent.Set();

            localPeerEvent.Set();

            Save();
        }

        static void Refresh()
        {
            while (!Client.Stop)
            {
                bool any;

                lock (peers)
                    any = peers.Any();

                if (!any)
                {
                    localPeerEvent.Reset();

                    localPeerEvent.WaitOne();
                }

                if (Client.Stop)
                    break;

                if (!queue.Any())
                    FillQueue();

                Peer peer = queue.Dequeue();

                Client.Stats.belowMinSentEvent.WaitOne(); //todo: or max confomr % de uso, ver outro uso (adicionar if Client.IsIdle use belowMax)

                if (Client.Stop)
                    break;

                //Probability by address distance to Local address
                var probabilityByAddressDistance = Math.Log(Addresses.EuclideanDistance(Client.LocalPeer.Address, peer.Address) * 100, TopAverageDistance * 100) - 1;

                if (double.IsNaN(probabilityByAddressDistance))
                    probabilityByAddressDistance = 1;

                var maxLatency = Latency.Average * 2;

                var latencyPercent = peer.Latency / maxLatency;

                var averagePercent = Latency.Average / maxLatency;

                var probabilityLatency = Math.Log(100 * latencyPercent, 100 * averagePercent) - 1;

                if (double.IsNaN(probabilityLatency))
                    probabilityLatency = 1;

                if (probabilityByMaxPeers > 0 && Utils.Roll((probabilityByMaxPeers + probabilityByAddressDistance + probabilityLatency) / 3d))
                {
                    Remove(peer);

                    totalAtFillQueue--;

                    probabilityByMaxPeers = (totalAtFillQueue - pParameters.PeerMaxItems) / (double)totalAtFillQueue;
                }
                else if (Utils.Roll(1 - probabilityByAddressDistance))
                {
                    new p2pRequest(RequestCommand.Peer, destinationPeer: peer).Enqueue();
                }


            }
        }

        static void FillQueue()
        {
            totalAtFillQueue = peers.Count();

            probabilityByMaxPeers = (totalAtFillQueue - pParameters.PeerMaxItems) / (double)totalAtFillQueue;

            if (probabilityByMaxPeers < (pParameters.MinPeerMaintenanceQueueSize / totalAtFillQueue))
            {
                aboveMaxPeersEvent.Reset();

                aboveMaxPeersEvent.WaitOne();
            }

            lock (peers)
            {
                queue = new Queue<Peer>(peers.OrderBy(x => Utils.Rand.Next()).Select(x => x.Value).Take(pParameters.PeerMaintenanceQueueSize));

                TopAverageDistance = peers.OrderBy(x => x.Key).Take(peers.Count() / pParameters.TopPeersPercent).Sum(x => x.Key) / (peers.Count() / pParameters.TopPeersPercent);
            }
        }

        static void Remove(Peer peer)
        {
            lock (peers)
                peers.Remove(Addresses.EuclideanDistance(Client.LocalPeer.Address, peer.Address));
        }

        #endregion

        #region Serialization

        static void Load()
        {
            if (!File.Exists(pParameters.peersPath))
            {
                if (File.Exists(pParameters.peersPath + ".new"))
                    File.Move(pParameters.peersPath + ".new", pParameters.peersPath);
                else if (File.Exists(pParameters.peersPath + ".old"))
                    File.Move(pParameters.peersPath + ".old", pParameters.peersPath);
                else
                {

                    //installer
                    //using (var f = new FileStream(AppDomain.CurrentDomain.FriendlyName, FileMode.Open, FileAccess.Read))
                    //{
                    //    f.Seek(-10 * 1024, SeekOrigin.End);

                    //    byte[] b = new byte[10 * 1024];

                    //    f.Read(b, 0, b.Length);

                    //    int count = BitConverter.ToInt32(b, 0);

                    //    AddPeersFromBytes(b.Skip(4).Take(count).ToArray());
                    //}


                    return;
                }
            }

            byte[] buffer = File.ReadAllBytes(pParameters.peersPath);

            LocalAddressDistance.Add(BitConverter.ToDouble(buffer, 0));

            LastAccess.Add(BitConverter.ToDouble(buffer, 8));

            Latency.Add(BitConverter.ToDouble(buffer, 16));

            buffer = buffer.Skip(24).ToArray();

            PeersFromBytes(buffer);
        }

        private static void PeersFromBytes(byte[] buffer)
        {
            int reg_size = Peers.reg_size;

            Peer[] pp = new Peer[buffer.Length / reg_size];

            for (int i = 0; i < buffer.Length / reg_size; i++)
            {
                var peer = FromBytes(buffer.Skip(i * (reg_size)).Take(reg_size).ToArray());

                var dist = Addresses.EuclideanDistance(Client.LocalPeer.Address, peer.Address);

                peers.Add(dist, peer);
            }
        }

        static void Save()
        {
            byte[] data;

            lock (peers)
            {
                var count = peers.Count();

                data = new byte[24 + Peers.reg_size * count]; //LocalAddresDistance + Last access + Latency = 8 + 8 + 8 =  24

                for (var i = 0; i < count; i++)
                    Peers.ToBytes(peers[i]).ToArray().CopyTo(data, 24 + i * Peers.reg_size);
            }

            string newFilename = pParameters.peersPath + ".new";


            BitConverter.GetBytes(LocalAddressDistance.Average).CopyTo(data, 0);

            BitConverter.GetBytes(LastAccess.Average).CopyTo(data, 8);

            BitConverter.GetBytes(Latency.Average).CopyTo(data, 16);

            File.WriteAllBytes(newFilename, data);

            if (File.Exists(pParameters.peersPath + ".old"))
                File.Delete(pParameters.peersPath + ".old");

            if (File.Exists(pParameters.peersPath))
                File.Move(pParameters.peersPath, pParameters.peersPath + ".old");

            File.Move(newFilename, pParameters.peersPath);
        }

        internal static void AddPeersFromBytes(byte[] request)
        {
            int c = request.Count() / Peers.reg_size;

            for (int i = 0; i < c; i++)
            {
                Peer peer = FromBytes(request.Skip(Peers.reg_size * i).Take(Peers.reg_size).ToArray());

                AddPeer(peer);
            }
        }

        internal static int reg_size =
          pParameters.addressSize +
          pParameters.ipv4Addresssize +
          sizeof(UInt16) + //port
          sizeof(Int64) + //last access
          sizeof(double); //last latency

        static internal IEnumerable<byte> ToBytes(Peer peer)
        {

            return peer.Address
                .Concat(Addresses.ToBytes(peer.EndPoint))
                .Concat(BitConverter.GetBytes(peer.LastAccess.ToBinary()))
                .Concat(BitConverter.GetBytes(peer.Latency))
                    .ToArray();
        }

        internal static byte[] ToBytes(List<Peer> peerList)
        {
            //byte[] result = new byte[peerList.Count() * Peers.reg_size];

            int i = 0;

            var result = new List<byte>();

            foreach (Peer p in peerList)
            {
                result.AddRange(ToBytes(p));
                //ToBytes(p).ToArray().CopyTo(result, i * Peers.reg_size);
                i++;
            }

            return result.ToArray();
        }

        internal static Peer FromBytes(byte[] data)
        {
            var offset = 0;

            var address = data.Take(pParameters.addressSize).ToArray();

            offset += pParameters.addressSize;

            var ip = new IPAddress(data.Skip(offset).Take(pParameters.ipv4Addresssize).ToArray());

            offset += pParameters.ipv4Addresssize;

            var port = BitConverter.ToUInt16(data, offset);

            offset += sizeof(UInt16);

            var endpoint = new IPEndPoint(ip, port);

            Peer peer = Peers.CreatePeer(endpoint, address);





            peer.LastAccess = DateTime.FromBinary(BitConverter.ToInt64(data, offset));

            offset += sizeof(Int64);

            peer.Latency = BitConverter.ToDouble(data, offset);


            return peer;
        }

        #endregion

        static void BurnYourDead()
        {
        }

        internal static void AddPeer(Peer peer)
        {
            lock (peers)
            {
                if ((peer.Address == null || Addresses.Equals(peer.Address, Addresses.zero) || !peers.Any(x => Addresses.Equals(peer.Address, x.Value.Address))) && !peer.EndPoint.Equals(Client.LocalPeer.EndPoint))
                {
                    var p2 = peers.FirstOrDefault(x => peer.EndPoint.Equals(x.Value.EndPoint)).Value;

                    if (p2 == null)
                    {
                        var dist = Addresses.EuclideanDistance(Client.LocalPeer.Address, peer.Address);

                        peers.Add(dist, peer);

                        if (peers.Count() > pParameters.PeerMaxItems * (1 + (pParameters.MinPeerMaintenanceQueueSize) / 100d))
                            aboveMaxPeersEvent.Set();

                        Peers.LocalAddressDistance.Add(dist);

                        Peers.BeginGetPeer(peer);

                        p2pRequest request = new p2pRequest(
                          command: RequestCommand.Peer,
                          address: Client.LocalPeer.Address,
                          originalPeer: Client.LocalPeer,
                          senderPeer: Client.LocalPeer,
                          destinationPeer: peer);

                        request.Enqueue();

                        localPeerEvent.Set();

                    }
                    else if (Addresses.Equals(p2.Address, Addresses.zero, true))
                    {
                        p2.Address = peer.Address;
                    }
                }
            }

        }

        internal static Peer CreateLocalPeer(IPEndPoint endpoint)
        {
            var peer = new Peer();

            peer.EndPoint = endpoint;

            peer.Address = Client.P2pAddress;

            return peer;
        }

        internal static Peer CreatePeer(IPEndPoint iPEndPoint, byte[] address = null)
        {
            Peer result = new Peer();

            result.Address = address ?? result.Address;

            result.EndPoint = iPEndPoint;

            result.LastAccess = DateTime.Now;

            Peers.LastAccess.Add(result.LastAccess.ToBinary());

            return result;
        }

        internal static Peer GetPeer(IPEndPoint remoteEndPoint, byte[] address = null)
        {
            Peer peer;

            if (address != null)
                lock (peers)
                    peer = peers.FirstOrDefault(x => Addresses.Equals(x.Value.Address, address)).Value;
            else
                lock (peers)
                    peer = peers.FirstOrDefault(x => x.Value.EndPoint.Equals(remoteEndPoint)).Value;

            if (peer != null)
                peer.Address = address ?? peer.Address;

            return peer;
        }

        internal static Peer[] GetPeers(
            byte[] closestToAddress = null,
            byte[] excludeOriginAddress = null,
            Peer[] excludeSenderPeer = null,
            int count = 10)
        {
            var result = new Peer[0];

            if (Client.Stop)
                return result;

            lock (peers)
                result = peers.OrderBy(x => Addresses.EuclideanDistance(x.Value.Address, closestToAddress)).Select(x => x.Value).Take(count * 2).ToArray();

            result.OrderBy(x => Utils.Rand.Next()).Take(count);

            return result;
        }

        internal static Peer GetPeer(
            byte[] closestToAddress = null,
            byte[] excludeOriginAddress = null,
            Peer[] excludeSenderPeer = null)
        {
            return
                GetPeers(closestToAddress, excludeOriginAddress, excludeSenderPeer, 1)
                    .FirstOrDefault();
        }



        internal static void BeginGetPeer(Peer peer)
        {
            if (!peer.endGetPeer)
                EndGetPeer(peer);

            peer.LastGetPeerRequisition = DateTime.Now;

            peer.endGetPeer = false;
        }

        internal static void EndGetPeer(Peer peer)
        {
            var diff = Math.Min(pParameters.peers_interval, DateTime.Now.Subtract(peer.LastGetPeerRequisition).TotalMilliseconds);

            peer.Latency = diff;

            Peers.Latency.Add(diff);

            peer.endGetPeer = true;
        }

        internal static bool AnyPeer()
        {
            lock (peers)
                return peers.Any();
        }
    }

    //command
    //    HttpGet = 542393671
    // case Command.HttpGet:
    //{
    //    string str = "HTTP/1.x 200 OK\r\n";
    //    str += "Content-Disposition: attachment; filename=windows.zip\r\n";
    //    str += "Content-Type: application/octet-stream\r\n";
    //    str += "Content-Length: 39450\r\n\r\n";

    //    byte[] bb = System.Text.Encoding.ASCII.GetBytes(str);

    //    state.socket.BeginSend(bb, 0, bb.Length, 0, new AsyncCallback(OnHttp), state);

    //    return;

    //}
}