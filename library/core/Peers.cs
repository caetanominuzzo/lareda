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
        static List<Peer> peers = new List<Peer>();

        internal static Queue<Peer> queue = new Queue<Peer>();

        internal static TimeCounter LocalAddressDistance = new TimeCounter(10, 10);

        internal static TimeCounter Latency = new TimeCounter(10, 10);

        internal static TimeCounter LastAccess = new TimeCounter(10, 10);

        internal static ManualResetEvent localPeerEvent = new ManualResetEvent(true);

        internal static ManualResetEvent aboveMaxPeersEvent = new ManualResetEvent(false);

        static double removingPeerProbabilityByMaxPeers = 0;

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

                Client.Stats.belowMinSentEvent.WaitOne();  //todo: or max confomr % de uso, ver outro uso

                if (Client.Stop)
                    break;

                var avgDistance = LocalAddressDistance.Average;

                //Probability by address distance to Local address
                var pL = Math.Log(Addresses.EuclideanDistance(Client.LocalPeer.Address, peer.Address), avgDistance);

                if (double.IsNaN(pL))
                    pL = 1;

                var lastAccess = DateTime.Now.Subtract(peer.LastAccess).TotalMinutes;

                var avgLastAccess = LastAccess.Average;

                //probability by last access
                var pA = Math.Log(lastAccess, avgLastAccess);

                if (double.IsNaN(pA))
                    pA = 1;

                //Probability by Latency
                var pR = Math.Log(peer.Latency, Latency.Average);

                if (double.IsNaN(pR))
                    pR = 1;

                if (removingPeerProbabilityByMaxPeers > 0 && Utils.Roll(removingPeerProbabilityByMaxPeers * pL * pA * pR))
                {
                    Remove(peer);
                }
            }
        }

        static void FillQueue()
        {

            var total = peers.Count();

            //Probability by total peers
            removingPeerProbabilityByMaxPeers = (total - pParameters.PeerMaxItems) / (double)total;

            if (removingPeerProbabilityByMaxPeers < .1)
            {
                aboveMaxPeersEvent.Reset();

                aboveMaxPeersEvent.WaitOne();
            }


            lock (peers)
                queue = new Queue<Peer>(peers.OrderBy(x => Utils.Rand.Next()).Take(pParameters.PeerMaintenanceQueueSize));
        }

        static void Remove(Peer peer)
        {
            lock (peers)
                peers.Remove(peer);
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

                peers.Add(peer);
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
            Log.Write("Addpeers:" + Utils.Points(request));

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
            Log.Write("to bytes-------------------------");

            Log.Write("address: " + Utils.Points(peer.Address) + Utils.ToBase64String(peer.Address), 1);

            Log.Write("endpoint: "+ Utils.Points(Addresses.ToBytes(peer.EndPoint)), 1);

            Log.Write("last ac: " + Utils.Points(BitConverter.GetBytes(peer.LastAccess.ToBinary())), 1);

            Log.Write("latency: " + Utils.Points(BitConverter.GetBytes(peer.Latency)), 1);

            Log.Write("everybody: " + Utils.Points(peer.Address
                .Concat(Addresses.ToBytes(peer.EndPoint))
                .Concat(BitConverter.GetBytes(peer.LastAccess.ToBinary()))
                .Concat(BitConverter.GetBytes(peer.Latency))
                    .ToArray()), 2);

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

            Log.Write("from bytes-------------------------");

            Log.Write("address: " + Utils.Points(peer.Address) + Utils.ToBase64String(peer.Address), 1);

            Log.Write("endpoint: " + Utils.Points(Addresses.ToBytes(peer.EndPoint)), 1);

            Log.Write("last ac: " + Utils.Points(BitConverter.GetBytes(peer.LastAccess.ToBinary())), 1);

            Log.Write("everybody: " + Utils.Points(data), 2);




            peer.LastAccess = DateTime.FromBinary(BitConverter.ToInt64(data, offset));

            offset += sizeof(Int64);

            peer.Latency = BitConverter.ToDouble(data, offset);



            Log.Write("latency: " + Utils.Points(BitConverter.GetBytes(peer.Latency)), 1);

            
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
                if ((peer.Address == null || !peers.Any(x => Addresses.Equals(peer.Address, Addresses.zero) && Addresses.Equals(peer.Address, x.Address))) && !peer.EndPoint.Equals(Client.LocalPeer.EndPoint))
                {
                    var p2 = peers.FirstOrDefault(x => peer.EndPoint.Equals(x.EndPoint));

                    if (p2 == null)
                    {
                        peers.Add(peer);


                        if (peers.Count() > pParameters.PeerMaxItems * 1.1)
                            aboveMaxPeersEvent.Set();

                        Peers.LocalAddressDistance.Add(Addresses.EuclideanDistance(Client.LocalPeer.Address, peer.Address));

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

        internal static Peer CreateLocalPeer()
        {
            var peer = new Peer();

            peer.EndPoint = new IPEndPoint(IPAddress.Loopback, Client.P2pPort);

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
                    peer = peers.FirstOrDefault(x => Addresses.Equals(x.Address, address));
            else
                lock (peers)
                    peer = peers.FirstOrDefault(x => x.EndPoint.Equals(remoteEndPoint));

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
                result = peers.OrderBy(x => Addresses.EuclideanDistance(x.Address, closestToAddress)).Take(count * 2).ToArray();

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