﻿//using LumiSoft.Net.STUN.Client;
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
            localEndPoint = new IPEndPoint(IPAddress.Any, Client.P2pPort);

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
                try
                {
                    IPEndPoint remoteEndPoint = null;

                    byte[] buffer = server.Receive(ref remoteEndPoint);

                    if (remoteEndPoint != null)
                    {
                        Log.Write(Client.LocalPeer.EndPoint.Port + " <<< " + remoteEndPoint.Port + " [" +
    ((RequestCommand)buffer[0]).ToString() + "] [" + Utils.ToSimpleAddress(buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray()) + "] [" + Utils.Points(buffer.Skip(pParameters.requestHeaderSize + pParameters.addressSize).Take(128).ToArray()) + "] [" + Utils.Points(buffer));

                       // Log.Write("<<<    " + remoteEndPoint.Port + "    " + Utils.Points(buffer.Take(128)));

                        var r = p2pRequest.CreateRequestFromReceivedBytes(remoteEndPoint, buffer);

                        p2pResponse.Process(r);

                        //ThreadPool.QueueUserWorkItem(new WaitCallback(p2pResponse.Process), new p2pRequest(remoteEndPoint, buffer));

                        Client.Stats.Received.Add(buffer.Length);
                    }
                }
                catch(Exception e) {
                    Log.Write(e.ToString());
                }
            }

            server.Close();
        }

        #endregion
    }
}

