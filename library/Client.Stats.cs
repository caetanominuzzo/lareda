using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace library
{
    partial class Client
    {
        public static class Stats
        {
            static Timer timer = new Timer(TimerTask, null, 0, 10);

            internal static ManualResetEvent belowMaxSentEvent = new ManualResetEvent(true);

            internal static ManualResetEvent belowMaxReceivedEvent = new ManualResetEvent(true);

            internal static ManualResetEvent belowMinSentEvent = new ManualResetEvent(true);

            internal static ManualResetEvent belowMinReceivedEvent = new ManualResetEvent(true);

            internal static bool IsAboveMaxSent { get { return !belowMaxSentEvent.WaitOne(0); } }

            internal static bool IsAboveMaxReceived { get { return !belowMaxReceivedEvent.WaitOne(0); } }

            internal static bool IsAboveMinSent { get { return !belowMinSentEvent.WaitOne(0); } }

            internal static bool IsAboveMinReceived { get { return !belowMinReceivedEvent.WaitOne(0); } }

            public static TimeCounter Sent = new TimeCounter(1, 10);

            public static TimeCounter Received = new TimeCounter(1, 10);

            internal static TimeCounter PresumedReceived = new TimeCounter(1, 10);

            static void TimerTask(object o)
            {
                if (IsAboveMaxSent && below_max_send())
                    belowMaxSentEvent.Set();
                else if (!below_max_send())
                    belowMaxSentEvent.Reset();

                if (IsAboveMaxReceived && below_max_received())
                    belowMaxReceivedEvent.Set();
                else if (!below_max_received())
                    belowMaxReceivedEvent.Reset();

                if (IsAboveMinSent && below_min_send())
                    belowMinSentEvent.Set();
                else if (!below_min_send())
                    belowMinSentEvent.Reset();

                if (IsAboveMinReceived && below_min_received())
                    belowMinReceivedEvent.Set();
                else if (!below_min_received())
                    belowMinReceivedEvent.Reset();
            }

            #region NetworkInterface

            static NetworkInterface networkInterface;

            static void GetInterface()
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ip.Address.Equals(Client.LocalPeer.EndPoint.Address))
                            {
                                networkInterface = ni;
                                break;
                            }
                        }
                    }
                }
            }

            static long bytes_received()
            {
                if (networkInterface == null)
                    GetInterface();

                if (networkInterface == null)
                    return 0;

                return networkInterface.GetIPv4Statistics().BytesReceived;
            }

            #endregion

            public static int max_upload = 100 * 1024;

            public static int max_download = 100 * 1024;

            public static bool below_max_send()
            {
                return Sent.TotalLastPeriod < max_upload;
            }

            public static bool below_max_received()
            {
                return Received.TotalLastPeriod + PresumedReceived.TotalLastPeriod < max_download;
            }

            public static bool below_min_send()
            {
                return Sent.TotalLastPeriod < max_upload * .01;
            }

            public static bool below_min_received()
            {
                return Received.TotalLastPeriod + PresumedReceived.TotalLastPeriod < max_download * .01;
            }


        }
    }
}
