using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    class p2pResponse
    {
        p2pRequest Request;

        p2pResponse(p2pRequest request)
        {
            Request = request;
        }

        internal static void Process(object o)
        {
            p2pResponse res = new p2pResponse((p2pRequest)o);

            res.Process();
        }

        void Process()
        {
            switch (Request.Command)
            {
                case RequestCommand.Peer:
                    ProcessPeer();
                    break;

                case RequestCommand.Packet:
                    ProcessPacket();
                    break;

                case RequestCommand.Metapackets:
                case RequestCommand.Hashs:
                    ProcessMetadata();
                    break;
            }
        }

        void ProcessPeer()
        {
            if (Client.LocalPeer.EndPoint.Address.Equals(IPAddress.Any))
                Client.LocalPeer = Request.OriginalPeer;

            if (Request.Data.Length > pParameters.addressSize)
            {
                Peers.EndGetPeer(Request.OriginalPeer);

                Peers.AddPeersFromBytes(Request.Data);
            }
            else
            {
                List<Peer> peers = Peers.GetPeers(
                    closestToAddress: Request.Address,
                    excludeOriginAddress: Request.Address,
                    excludeSenderPeer: new[] { Request.OriginalPeer },
                    count: pParameters.GetPeerCountReturn).ToList();

                Peers.AddPeer(Request.OriginalPeer);

                peers.Add(Client.LocalPeer);

                p2pRequest request = new p2pRequest(
                    command: RequestCommand.Peer,
                    originalPeer: Request.SenderPeer,
                    senderPeer: Client.LocalPeer,
                    destinationPeer: Request.OriginalPeer,
                    data: Peers.ToBytes(peers));

                request.Enqueue();
            }
        }

        void ProcessPacket()
        {
            if (Request.Data.Length > pParameters.addressSize)
            {
                Packets.Add(Request.Address, Request.Data.Skip(pParameters.addressSize).ToArray(), Request.OriginalPeer);
            }
            else
            {
                var data = Packets.Get(Request.Address);

                if (data != null)
                {
                    p2pRequest request = new p2pRequest(
                       command: RequestCommand.Packet,
                       address: Request.Address,
                       originalPeer: Request.OriginalPeer,
                       senderPeer: Client.LocalPeer,
                       destinationPeer: Request.OriginalPeer,
                       data: data);


                    request.Send();

                }
                else
                {
                    //todo: Propagate 
                    //n vezes se origin == sender senão uma probabilidade conforme a distancia do endereço local até o endereço pesquisado.
                }
            }
        }

        void ProcessMetadata()
        {
            if (Utils.ToSimpleAddress(Request.Address) == "129")
            {

            }

            if (Request.Data.Length > pParameters.addressSize)
            {
                //Packets.Add(Request.Header.Address, Request.Data, Request.Header.OriginPeer);

                var metapackets = MetaPackets.FromBytes(Request.Data.Skip(pParameters.addressSize).ToArray());

                if (null != metapackets)
                {
                    foreach (var m in metapackets)
                    {
                        m.Type = Request.Command == RequestCommand.Hashs ? MetaPacketType.Hash : MetaPacketType.Link;

                        MetaPackets.Add(m);
                    }

                    Client.SearchReturn(Request.Address, Request.Command == RequestCommand.Hashs ? MetaPacketType.Hash : MetaPacketType.Link, metapackets);
                }
            }
            else
            {

                var m = MetaPackets.LocalSearch(Request.Address, Request.Command == RequestCommand.Hashs ? MetaPacketType.Hash : MetaPacketType.Link);

                if (m != null && m.Any())
                {
                    if (m.Any(x => x.Hash != null && !Addresses.Equals(x.Hash, Addresses.zero, true)))
                    {

                    }

                    var res = new p2pRequest(
                        Request.Command, Request.Address, Client.LocalPeer, Client.LocalPeer, Request.OriginalPeer, MetaPackets.ToBytes(m));

                    res.Enqueue();
                }
            }

            //    byte[] address = Utils.ReadBytes(Request.Data, Parameters.requestHeaderSize);

            //    byte[] data = Utils.ReadBytes(Request.Data, Parameters.requestHeaderSize + 4 + address.Length).ToArray();

            //    if (data.Count() == 0)
            //    {
            //        ValueHits addresses = LocalIndex.SearchMetadataAddressesByValue(address);

            //        if (addresses == null || !addresses.Any())
            //        {
            //            p2pRequest.Enqueue(
            //                senderPeer: Request.SenderPeer,
            //                address: BitConverter.GetBytes(address.Length).Concat(address).ToArray(),
            //                command: RequestCommand.Metadata,
            //                returnPeer: Request.Header.Peer);
            //        }
            //        else
            //        {
            //            foreach (ValueHitsItem item in addresses)
            //            {
            //                p2pRequest.Enqueue(
            //                    senderPeer: Request.SenderPeer,
            //                    originPeer: Request.Header.Peer ?? Request.OriginPeer,
            //                    address: item.Value,
            //                    data: LocalPackets.Get(item.Value));
            //            }
            //        }
            //    }
            //    else
            //    {
            //        LocalPackets.Add(Utils.GetAddress(), data, Request.OriginPeer);
            //    }
        }
    }
}