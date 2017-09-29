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
        internal RequestCommand Command = RequestCommand.Packet;

        internal Peer OriginalPeer;

        internal byte[] Address;

        internal Peer SenderPeer;

        internal Peer DestinationPeer;

        internal byte[] Data;

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
            Peer originalPeer = null,
            Peer senderPeer = null,
            Peer destinationPeer = null,
            byte[] data = null)
        {
            Command = command;

            Address = address;

            OriginalPeer = originalPeer;

            SenderPeer = senderPeer;

            DestinationPeer = destinationPeer;

            Data = data;

            createTime = DateTime.Now;
        }

        internal static p2pRequest CreateRequestFromReceivedBytes(IPEndPoint endpoint, byte[] buffer)
        {
            //var header = p2pRequestHeader.CreateFromReceivedBytes(buffer);

            byte[] address = buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray();

            IPEndPoint originEndPoint = Addresses.FromBytes(buffer.Skip(pParameters.requestHeaderParamsSize).ToArray());

            Peer originalPeer = null;

            if (originEndPoint != null)
                originalPeer = Peers.CreatePeer(originEndPoint, null);

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
                originalPeer: originalPeer,
                address: address,
                senderPeer: senderPeer,
                destinationPeer: Client.LocalPeer,
                data: buffer.Skip(pParameters.requestHeaderSize).ToArray());

            return result;
        }

        #region Thread Refresh

        internal static void Start()
        {
            Thread thread = new Thread(Refresh);

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
            if (DestinationPeer == null)
            {
                DestinationPeer = Peers.GetPeer(
                    closestToAddress: Address,
                    excludeOriginAddress: Address,
                    excludeSenderPeer: new[] { OriginalPeer });

                if (DestinationPeer == null)
                    return false;

                if (Client.Stop)
                    return false;
            }

            var data = ToBytes().
                 Concat(Address ?? bytes_empty).
                Concat(Data ?? bytes_empty).ToArray();

            Log.Add(Log.LogTypes.P2p, Log.LogOperations.Outgoing | Log.FromCommand(Command), new { Port = DestinationPeer.EndPoint.Port, Address = Address ?? Data, Data = null != Data && Data.Length > 0 });

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

            if (OriginalPeer != null)
                Addresses.ToBytes(OriginalPeer.EndPoint).CopyTo(b, pParameters.requestHeaderParamsSize);

            return b;
        }


        static void ThreadSend(IPEndPoint remoteEndPoint, byte[] data)
        {
            if(data[0] == (int)RequestCommand.Packet)
            {

            }

            UdpClient u = null;

            if (null == u)
            {
                u = new UdpClient();

                u.Client.ExclusiveAddressUse = false;

                u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                u.Client.Bind(new IPEndPoint(IPAddress.Any, Client.P2pEndpoint.Port));
            }

            Client.Stats.Sent.Add(data.Length);

            try
            {
                Log.Add(Log.LogTypes.P2p, Log.LogOperations.Outgoing, data, remoteEndPoint);

                u.Send(data, data.Length, remoteEndPoint);
            }
            catch (Exception e)
            {
                Log.Add(Log.LogTypes.Application, Log.LogOperations.Exception, new { remoteEndPoint, Exception = e.ToString() });
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
