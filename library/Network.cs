using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace library
{
    public static class Network
    {
        public static void Configure()
        {
           // ThreadPool.QueueUserWorkItem(new WaitCallback(configure));
        }

        static void configure(object o)
        {
            //NATUPNPLib.UPnPNATClass upnpnat = new NATUPNPLib.UPnPNATClass();

            //NATUPNPLib.IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            //NATUPNPLib.IStaticPortMapping map = mappings.Add(Client.P2pPort, "UDP", Client.P2pPort, "192.168.200.11", true, "UDP1");

            //string m = map.Description + map.Enabled + map.ExternalIPAddress + ":" + map.ExternalPort + "+" + map.InternalPort + map.Protocol + "  " + map.ToString();
        }


    }
}
