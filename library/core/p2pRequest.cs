using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace library
{
    class p2pRequest
    {
        internal List<p2pRequest> parents = new List<p2pRequest>();

        internal List<byte[]> results = new List<byte[]>();

        internal RequestCommand Command = RequestCommand.Packet;

        internal Peer OriginalPeer;

        internal byte[] Address;

        internal Peer SenderPeer;

        internal Peer DestinationPeer;

        internal byte[] Data;

        internal byte? TTL;

        internal static byte[] bytes_empty = new byte[0];

        static ManualResetEvent enqueueEvent = new ManualResetEvent(false);

        static Queue<p2pRequest> queue = new Queue<p2pRequest>();

        DateTime createTime;

        bool Expired()
        {
            return DateTime.Now.Subtract(createTime).TotalMilliseconds > pParameters.response_timeout;
        }

        internal p2pRequest(
            RequestCommand command = RequestCommand.Peer,
            byte[] address = null,
            byte? ttl = null,
            Peer originalPeer = null,
            Peer senderPeer = null,
            Peer destinationPeer = null,
            byte[] data = null)
        {
            Command = command;

            if (ttl.HasValue)
                TTL = ttl;
            else
                TTL = pParameters.TTL;

            Address = address;

            OriginalPeer = originalPeer;

            SenderPeer = senderPeer;

            DestinationPeer = destinationPeer;

            Data = data;

            createTime = DateTime.Now;
        }

        internal static p2pRequest CreateRequestFromReceivedBytes(IPEndPoint endpoint, byte[] buffer)
        {
            byte[] address = buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray();

            IPEndPoint originalEndPoint = Addresses.FromBytes(buffer.Skip(pParameters.requestHeaderParamsSize).ToArray());

            Peer originalPeer = null;

            if (originalEndPoint != null)
                originalPeer = Peers.CreatePeer(originalEndPoint, null);

            if (address != null && address.Length == 0)
                address = null;

            var senderPeer = Peers.GetPeer(endpoint,
                buffer[0] == (int)RequestCommand.Peer &&
                    !Addresses.Equals(Client.LocalPeer.Address, address) ?
                    address :
                    null);

            if (senderPeer == null)
                senderPeer = Peers.CreatePeer(endpoint, buffer[0] == (int)RequestCommand.Peer &&
                    !Addresses.Equals(Client.LocalPeer.Address, address) ?
                    address :
                    null);

            if (originalPeer == null)
                originalPeer = senderPeer;

            var result = new p2pRequest(
                command: (RequestCommand)buffer[0],
                ttl: buffer[1],
                originalPeer: originalPeer,
                address: address,
                senderPeer: senderPeer,
                destinationPeer: Client.LocalPeer,
                data: buffer.Skip(pParameters.requestHeaderSize).ToArray());

            result.parents = p2pServer.requests_received.Where(x => Addresses.Equals(x.CachedValue.Address, address)).Select(x => x.CachedValue).ToList();

            return result;
        }

        #region Thread Refresh

        internal static void Start()
        {
            Thread thread = new Thread(Refresh);

            thread.Name = "p2pRequest";

            thread.Start();
        }

        internal static void Stop()
        {
            enqueueEvent.Set();

            Client.Stats.belowMaxSentEvent.Set();
        }

        static void Refresh()
        {
            while (!Client.Stop)
            {
                enqueueEvent.WaitOne();

                if (Client.Stop)
                    break;

                Client.Stats.belowMaxSentEvent.WaitOne();

                if (Client.Stop)
                    break;

                Dequeue();
            }
        }

        #endregion

        static void Dequeue()
        {
            lock (queue)
            {
                while (queue.Any())
                {
                    p2pRequest request = queue.Dequeue();

                    while (DateTime.Now.Subtract(request.createTime).TotalMilliseconds > pParameters.time_out && queue.Any())
                        request = queue.Dequeue();

                    if (DateTime.Now.Subtract(request.createTime).TotalMilliseconds > pParameters.time_out)
                        break;

                    if (Client.Stop)
                        break;

                    request.Send();
                }

                enqueueEvent.Reset();
            }
        }

        internal void Enqueue()
        {
            lock (queue)
                queue.Enqueue(this);

            enqueueEvent.Set();
        }

        internal bool Send()
        {
            if (Command != RequestCommand.Peer)
            {
                var address = (Address ?? bytes_empty);

                if (address.Length == 0 && null != Data)
                    address = Data.Take(pParameters.addressSize).ToArray();

                if (null != Data && Data.Length == pParameters.addressSize)
                    lock (p2pServer.requests_sent)
                    {
                        if (p2pServer.requests_sent.Any(x => Addresses.Equals(x.CachedValue, address)))
                            return true;

                        p2pServer.requests_sent.Add(address);
                    }
            }

            if (DestinationPeer == null)
            {
                DestinationPeer = Peers.GetPeer(
                    closestToAddress: Address,
                    excludeOriginAddress: Address,
                    excludeSenderPeer: parents.Select(x => x.SenderPeer).Concat(new[] { OriginalPeer }).ToArray());

                if (DestinationPeer == null)
                    return false;

                if (Client.Stop)
                    return false;
            }

            var data = ToBytes().
                 Concat(Address ?? bytes_empty).
                Concat(Data ?? bytes_empty).ToArray();

            Log.Add(Log.LogTypes.P2p, Log.LogOperations.Outgoing | Log.FromCommand(Command), new { Port = DestinationPeer.EndPoint.Port, Address = Address ?? Data, Data = null != Data && Data.Length > pParameters.addressSize });

            //Client.LocalPeer.EndPoint.Port + " >>> " + DestinationPeer.EndPoint.Port + " [" +
            //    Command.ToString() + "] [" + Utils.ToSimpleAddress(Address != null && Address.Length > 0 ? Address : Data), 

            //    Command == RequestCommand.Packet? Log.LogTypes.p2pOutgoingPackets :
            //        Command == RequestCommand.Hashs ? Log.LogTypes.p2pOutgoingHash :
            //            Command == RequestCommand.Metapackets ? Log.LogTypes.p2pOutgoingMetapackets :
            //                Command == RequestCommand.Peer ? Log.LogTypes.p2pOutgoingPeers : Log.LogTypes.None

            //    );

            //if(Utils.ToBase64String((Address ?? bytes_empty).Concat(Data ?? bytes_empty).ToArray()) == "3DnFpsP2xPTUm9L4G++gLseAGI6QBqGWA5YKLUIHEoU=")
            //{

            //}

            ThreadSend(DestinationPeer.EndPoint, data);

            return true;
        }

        internal IEnumerable<byte> ToBytes()
        {
            byte[] b = new byte[pParameters.requestHeaderSize];

            b[0] = (byte)Command;

            if (TTL.HasValue)
                b[1] = TTL.Value;
            else
                b[1] = pParameters.TTL;

            Addresses.ToBytes(Client.LocalPeer.EndPoint).CopyTo(b, pParameters.requestHeaderParamsSize);

            return b;
        }

        static UdpClient u = null;

        static void ThreadSend(IPEndPoint remoteEndPoint, byte[] data)
        {
            if (data[0] == (int)RequestCommand.Packet)
            {

            }



            //if (null == u)
            //{
            //    u = new UdpClient();

            //    u.Client.ExclusiveAddressUse = false;

            //    u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            //    u.Client.Bind(new IPEndPoint(IPAddress.Any, Client.P2pEndpoint.Port));
            //}

            Client.Stats.Sent.Add(data.Length);

            try
            {
                var i = p2pServer.SocketTcpSend(data, data.Length, remoteEndPoint);

                Log.Add(Log.LogTypes.P2p, Log.LogOperations.Outgoing, new { remoteEndPoint = remoteEndPoint.ToString(), Wrote = i });

                //u.Send(data, data.Length, remoteEndPoint);
            }
            catch (Exception e)
            {
                Log.Add(Log.LogTypes.Application, Log.LogOperations.Exception, new { Exception = e.ToString() });
            }
        }
    }

    enum RequestCommand
    {
        None = 0,
        Packet = 1,
        Metapackets = 2,
        Hashs = 3,
        Peer = 4
    }
}
