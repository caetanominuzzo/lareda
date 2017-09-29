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
    static class p2pServer
    {
        static UdpClient server;

        static IPEndPoint localEndPoint;

        #region Thread Refresh

        static Thread thread;

        internal static void Start()
        {
            thread = new Thread(Configure);

            thread.Start();
        } 

        internal static void Stop()
        {
            server.Close();
        }

        static void Configure()
        {
            localEndPoint = new IPEndPoint(IPAddress.Any, Client.P2pEndpoint.Port);

            server = new UdpClient();

            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

            server.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

            server.Client.ExclusiveAddressUse = false;

            server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            server.Client.Bind(localEndPoint);

            // Client.localPeer.EndPoint = STUN_Client.Query("stunserver.org", 3478, client.Client).PublicEndPoint;

            ThreadReceive();
        }

        static void ThreadReceive()
        {
            while (!Client.Stop)
            {
                IPEndPoint remoteEndPoint = null;
                try
                {

                    byte[] buffer = server.Receive(ref remoteEndPoint);

                    if (remoteEndPoint != null)
                    {
                        var command = (RequestCommand)buffer[0];

                        if(command == RequestCommand.Packet)
                        {

                        }
                         
                        Log.Add(Log.LogTypes.P2p, Log.LogOperations.Incoming | Log.FromCommand(command), new { Port = remoteEndPoint.Port, Address = buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray(), Data = buffer.Length > pParameters.requestHeaderSize + pParameters.addressSize });

                        var r = p2pRequest.CreateRequestFromReceivedBytes(remoteEndPoint, buffer);

                        p2pResponse.Process(r);

                        //ThreadPool.QueueUserWorkItem(new WaitCallback(p2pResponse.Process), new p2pRequest(remoteEndPoint, buffer));

                        Client.Stats.Received.Add(buffer.Length);
                    }
                }
                catch (Exception e)
                {
                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Exception, new { Endpoint = (null == remoteEndPoint?string.Empty : remoteEndPoint.ToString()), Exception = e.ToString() });
                }
            }

            server.Close();
        }

        #endregion
    }
}