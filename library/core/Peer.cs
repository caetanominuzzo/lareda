using System;
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
    partial class Peer
    {
        internal byte[] Address = new byte[pParameters.addressSize];

        internal IPEndPoint EndPoint;

        internal double Latency = 0;

        internal DateTime LastAccess = DateTime.MinValue;

        internal DateTime LastGetPeerRequisition = DateTime.MinValue;

        internal bool endGetPeer;

        internal Peer()
        {
            LastAccess = DateTime.Now;

        }

        public string Serialize()
        {
            return string.Concat(
                string.Join(".", Address),
                ":\t",
                string.Join(".", EndPoint.Address.GetAddressBytes()),
                ":\t",
                EndPoint.Port);
        }
    }
}
