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
            if (Client.Stats.IsAboveMinSent) //todo: or max confomr % de uso, ver outro uso (adicionar if Client.IsIdle use belowMax)
                return;

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
                    closestToAddress:       Request.Address,
                    excludeOriginAddress:   Request.Address,
                    excludeSenderPeer:      new[] { Request.OriginalPeer },
                    count:                  pParameters.GetPeerCountReturn).ToList();

                Peers.AddPeer(Request.OriginalPeer);

                peers.Add(Client.LocalPeer);

                p2pRequest request = new p2pRequest(
                    command:         RequestCommand.Peer,
                    originalPeer: Request.SenderPeer,
                    senderPeer: Client.LocalPeer,
                    destinationPeer: Request.OriginalPeer,
                    data:            Peers.ToBytes(peers));

                request.Enqueue();
            }
        }

        void ProcessPacket()
        {
            if (Request.Data.Length > pParameters.addressSize)
            {
                Packets.Add(Request.Address, Request.Data.Skip(pParameters.addressSize).ToArray(), Request.OriginalPeer);

                var data_hash = Utils.ComputeHash(Request.Data, 0, Request.Data.Length);

                lock("a")
                foreach (var r in Request.parents)
                {
                    if (!r.results.Any(x => Addresses.Equals(x, data_hash, true)))
                    {
                        if(data_hash == null)
                        {

                        }
                        r.results.Add(data_hash);

                        var res = new p2pRequest(
                            command: Request.Command,
                            address: Request.Address,
                            originalPeer: Client.LocalPeer,
                            senderPeer: Client.LocalPeer,
                            destinationPeer: r.OriginalPeer,
                            data: Request.Data);

                        res.Enqueue();
                    }
                }
            }
            else
            {
                var data = Packets.Get(Request.Address);

                if (null != data)
                {
                    p2pRequest request = new p2pRequest(
                       command:         RequestCommand.Packet,
                       address:         Request.Address,
                       originalPeer:    Client.LocalPeer,
                       senderPeer:      Client.LocalPeer,
                       destinationPeer: Request.OriginalPeer,
                       data:            data);

                    request.Send();
                }
              //  else
                {
                    //todo: Propagate 
                    //Se originalPeer for preenchido (propagação rapida - retorno direto), nao salva cache de quem pesquisou;
                    //se peer Original == Any ao propagar salvar peer original num cache, ao receber um retorno de packet verifica se esta nesta lista e retona para o peer original.

                    //n vezes se origin == sender senão uma probabilidade conforme a distancia do endereço local até o endereço pesquisado.

                    if (Request.TTL < 1)
                        return;

                    Request.TTL--;

                    var destinationPeer = Peers.GetPeer(
                                           closestToAddress: Request.Address,
                                           excludeSenderPeer: new Peer[] { Request.SenderPeer, Request.OriginalPeer },
                                           excludeOriginAddress: Client.LocalPeer.Address);

                    if (null == destinationPeer)
                        return;

                    p2pRequest request = new p2pRequest(
                        ttl: Request.TTL,
                        address: Request.Address,
                        command: Request.Command,
                        originalPeer: Client.LocalPeer,
                        senderPeer: Client.LocalPeer,
                        destinationPeer: destinationPeer
                        //data: MetaPackets.ToBytes(new Metapacket[] { metapacket })
                        );

                    request.Enqueue();

                }
            }
        }

        void ProcessMetadata()
        {
            if (Request.Data.Length > pParameters.addressSize)
            {
                var metapackets = MetaPackets.FromBytes(Request.Data.Skip(pParameters.addressSize * 1).ToArray());

                if (null != metapackets)
                {
                    foreach (var m in metapackets)
                    {
                        m.Type = Request.Command == RequestCommand.Hashs ? MetaPacketType.Hash : MetaPacketType.Link;

                        MetaPackets.Add(m, Request.OriginalPeer);
                    }

                    Client.SearchReturn(Request.Address, Request.Command == RequestCommand.Hashs ? MetaPacketType.Hash : MetaPacketType.Link, metapackets);
                }

                var data_hash = Utils.ComputeHash(Request.Data, 0, Request.Data.Length);

                lock("a")
                foreach (var r in Request.parents)
                {
                    if (!r.results.Any(x => Addresses.Equals(x, data_hash, true)))
                    {
                        var res = new p2pRequest(
                            command: Request.Command,
                            address: Request.Address,
                            originalPeer: Client.LocalPeer,
                            senderPeer: Client.LocalPeer,
                            destinationPeer: r.OriginalPeer,
                            data: Request.Data);

                        res.Enqueue();
                    }
                }
            }
            else
            {
                var m = MetaPackets.LocalSearch(Request.Address, Request.Command == RequestCommand.Hashs ? MetaPacketType.Hash : MetaPacketType.Link);

                if (m != null && m.Any())
                {
                    var res = new p2pRequest(
                        command:         Request.Command, 
                        address:         Request.Address, 
                        originalPeer:    Client.LocalPeer, 
                        senderPeer:      Client.LocalPeer, 
                        destinationPeer: Request.OriginalPeer, 
                        data:            MetaPackets.ToBytes(m));

                    res.Enqueue();
                }
                //else
                {
                    if (Request.TTL < 1)
                        return;

                    Request.TTL--;

                    //todo: Propagate 
                    //n vezes se origin == sender senão uma probabilidade conforme a distancia do endereço local até o endereço pesquisado.
                    var destinationPeer = Peers.GetPeer(
                                       closestToAddress: Request.Address,
                                       excludeSenderPeer: Request.parents.Select(x => x.OriginalPeer).Concat(new Peer[] { Request.SenderPeer, Request.OriginalPeer }).ToArray(),
                                       excludeOriginAddress: Client.LocalPeer.Address);

                    if (null == destinationPeer)
                        return;

                    p2pRequest request = new p2pRequest(
                        ttl: Request.TTL,
                        address: Request.Address,
                        command: Request.Command,
                        originalPeer: Client.LocalPeer,
                        senderPeer: Client.LocalPeer,
                        destinationPeer: destinationPeer
                        //data: MetaPackets.ToBytes(new Metapacket[] { metapacket })
                        );

                    request.Enqueue();
                }
            }
        }
    }
}