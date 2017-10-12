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
        static UdpClient UdpServer;

        static TcpListener TcpServer;

        static Socket SocketTcpServer;

        static IPEndPoint localEndPoint;

        #region Thread Refresh

        static Thread thread;

        internal static void Start()
        {
            thread = new Thread(SocketTcpConfigure);

            thread.Start();
        }

        internal static void Stop()
        {
            //UdpServer.Close();

            SocketTcpServer.Close();
        }

        static void UdpConfigure()
        {
            localEndPoint = new IPEndPoint(IPAddress.Any, Client.P2pEndpoint.Port);

            UdpServer = new UdpClient(AddressFamily.InterNetwork);

            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

            UdpServer.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

            UdpServer.Client.ExclusiveAddressUse = false;

            UdpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            UdpServer.AllowNatTraversal(true);

            UdpServer.MulticastLoopback = true;

            //UdpServer.DontFragment = true;

            UdpServer.Client.Bind(localEndPoint);

            UdpServer.Client.ReceiveBufferSize = pParameters.packetSize;

            UdpServer.Client.SendBufferSize = pParameters.packetSize;

            // Client.localPeer.EndPoint = STUN_Client.Query("stunserver.org", 3478, client.Client).PublicEndPoint;

            UdpThreadReceive();
        }

        static void SocketTcpConfigure()
        {
            localEndPoint = new IPEndPoint(IPAddress.Any, Client.P2pEndpoint.Port);

            SocketTcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketTcpServer.Bind(localEndPoint);

            SocketTcpServer.ReceiveBufferSize = pParameters.packetSize;

            SocketTcpServer.SendBufferSize = pParameters.packetSize;

            SocketTcpServer.Listen(100);

            // Client.localPeer.EndPoint = STUN_Client.Query("stunserver.org", 3478, client.Client).PublicEndPoint;

            SocketTcpThreadReceive();
        }

        static void TcpConfigure()
        {
            localEndPoint = new IPEndPoint(IPAddress.Any, Client.P2pEndpoint.Port);

            TcpServer = new TcpListener(localEndPoint);

            TcpServer.Start();

            // Client.localPeer.EndPoint = STUN_Client.Query("stunserver.org", 3478, client.Client).PublicEndPoint;

            TcpThreadReceive();
        }


        static void UdpThreadReceive()
        {
            while (!Client.Stop)
            {
                IPEndPoint remoteEndPoint = null;
                try
                {
                    byte[] buffer = UdpServer.Receive(ref remoteEndPoint);

                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Incoming, new { a = 1 });

                    if (remoteEndPoint != null)
                    {
                        var command = (RequestCommand)buffer[0];

                        var r = p2pRequest.CreateRequestFromReceivedBytes(remoteEndPoint, buffer);

                        ThreadPool.QueueUserWorkItem(new WaitCallback(p2pResponse.Process), r);

                        Log.Add(Log.LogTypes.P2p, Log.LogOperations.Incoming | Log.FromCommand(command), new { Port = remoteEndPoint.Port, Address = buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray(), Data = buffer.Length > pParameters.requestHeaderSize + pParameters.addressSize });

                        Client.Stats.Received.Add(buffer.Length);
                    }
                }
                catch (Exception e)
                {
                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Exception, new { Endpoint = (null == remoteEndPoint ? string.Empty : remoteEndPoint.ToString()), Exception = e.ToString() });
                }
            }

            UdpServer.Close();
        }

        static void SocketTcpThreadReceive()
        {
            while (!Client.Stop)
            {
                IPEndPoint remoteEndPoint = null;
                try
                {
                    byte[] buffer1 = new byte[pParameters.packetSize];

                    var s = SocketTcpServer.Accept();

                    var buffer = ReceiveAll(s);

                    remoteEndPoint = (IPEndPoint)s.RemoteEndPoint;

                    var count = buffer.Length;

                    //var buffer = new byte[count];

                    //Buffer.BlockCopy(buffer1, 0, buffer, 0, count);

                    s.Close();

                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Incoming, new { a = 1 });

                    if (remoteEndPoint != null)
                    {
                        var command = (RequestCommand)buffer[0];

                        var r = p2pRequest.CreateRequestFromReceivedBytes(remoteEndPoint, buffer);

                        ThreadPool.QueueUserWorkItem(new WaitCallback(p2pResponse.Process), r);

                        Log.Add(Log.LogTypes.P2p, Log.LogOperations.Incoming | Log.FromCommand(command), new { Port = remoteEndPoint.Port, Address = buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray(), Data = buffer.Length > pParameters.requestHeaderSize + pParameters.addressSize });

                        Client.Stats.Received.Add(buffer.Length);
                    }
                }
                catch (Exception e)
                {
                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Exception, new { Endpoint = (null == remoteEndPoint ? string.Empty : remoteEndPoint.ToString()), Exception = e.ToString() });
                }
            }

            SocketTcpServer.Close();
        }

        public static byte[] ReceiveAll(Socket socket)
        {
            var buffer = new List<byte>();

            while (socket.Available > 0)
            {
                var currByte = new Byte[1];
                var byteCounter = socket.Receive(currByte, currByte.Length, SocketFlags.None);

                if (byteCounter.Equals(1))
                {
                    buffer.Add(currByte[0]);
                }
            }

            return buffer.ToArray();
        }

        static void TcpThreadReceive()
        {
            var bytes = new byte[pParameters.packetSize];

            while (!Client.Stop)
            {
                IPEndPoint remoteEndPoint = null;
                try
                {

                    var client = TcpServer.AcceptTcpClient();

                    remoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;


                    NetworkStream stream = client.GetStream();

                    int i;

                    byte[] buffer = null;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            ms.Write(bytes, 0, i);
                        }

                        client.Close();

                        buffer = ms.ToArray();

                    }

                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Incoming, new { a = 1 });

                    var command = (RequestCommand)buffer[0];

                    var r = p2pRequest.CreateRequestFromReceivedBytes(remoteEndPoint, buffer);

                    ThreadPool.QueueUserWorkItem(new WaitCallback(p2pResponse.Process), r);

                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Incoming | Log.FromCommand(command), new { Port = remoteEndPoint.Port, Address = buffer.Skip(pParameters.requestHeaderSize).Take(pParameters.addressSize).ToArray(), Data = buffer.Length > pParameters.requestHeaderSize + pParameters.addressSize });

                    Client.Stats.Received.Add(buffer.Length);

                    // Thread.Sleep(400);
                }
                catch (Exception e)
                {
                    Log.Add(Log.LogTypes.P2p, Log.LogOperations.Exception, new { Endpoint = (null == remoteEndPoint ? string.Empty : remoteEndPoint.ToString()), Exception = e.ToString() });
                }
            }
        }

        public static void TcpThreadReceiveCallback(IAsyncResult ar)
        {

        }

        internal static int TcpSend(byte[] data, int length, IPEndPoint endPoint)
        {
            var client = new TcpClient();

            client.Connect(endPoint);

            var stream = client.GetStream();

            stream.Write(data, 0, length);

            stream.Close();

            client.Close();

            return 0;
        }

        internal static int UdpSend(byte[] data, int length, IPEndPoint endPoint)
        {
            var i = UdpServer.Send(data, length, endPoint);

            return i;
        }

        internal static int SocketTcpSend(byte[] data, int length, IPEndPoint endPoint)
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            s.Connect(endPoint);

            var i = s.Send(data);

            s.Close();

            return i;
        }

        #endregion
    }
}