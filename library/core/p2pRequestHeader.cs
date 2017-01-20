using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    class p2pRequestHeader
    {
        internal RequestCommand Command = RequestCommand.Packet;

        internal Peer OriginPeer;

        internal byte[] Address;

        internal p2pRequestHeader(
            RequestCommand command = RequestCommand.Packet,
            Peer originPeer = null,
            byte[] address = null)
        {
            Command = command;

            OriginPeer = originPeer;

            Address = address;
        }

        internal static p2pRequestHeader CreateFromReceivedBytes(byte[] buffer)
        {
            p2pRequestHeader result = new p2pRequestHeader();

            result.Command = (RequestCommand)buffer[0];

            IPEndPoint endPoint = Addresses.FromBytes(buffer.Skip(pParameters.requestHeaderParamsSize).ToArray());

            if(endPoint != null)
                result.OriginPeer = Peers.GetPeer(endPoint);

            byte[] address = buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray();

            if (address != null && address.Length == 0)
                address = null;

            result.Address = address;

            return result;
        }

        static byte[] ToBytes(RequestCommand command)
        {
            byte[] b = new byte[pParameters.requestHeaderSize];

            b[0] = (byte)command;

            return b;
        }

        internal IEnumerable<byte> ToBytes()
        {
            byte[] b = ToBytes(Command);

            if (OriginPeer != null)
                Addresses.ToBytes(OriginPeer.EndPoint).CopyTo(b, pParameters.requestHeaderParamsSize);

            return b;
        }
    }

   
}
