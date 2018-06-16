using Open.Nat;
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
            //ThreadPool.QueueUserWorkItem(new WaitCallback(configure));

            Task.Run(configure);
        }

        static async Task configure() 
        {
            var discoverer = new NatDiscoverer();
             
            var cts = new CancellationTokenSource(10000);

            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            var ip = await device.GetExternalIPAsync(); 

            //Client.LocalPeer.EndPoint = new System.Net.IPEndPoint(ip, Client.LocalPeer.EndPoint.Port);


            var list = await device.GetAllMappingsAsync();

            
            //await device.CreatePortMapAsync(new Mapping(Protocol.Udp, Client.P2pEndpoint.Port, Client.P2pEndpoint.Port, pParameters.AppName));

            //NATUPNPLib.UPnPNATClass upnpnat = new NATUPNPLib.UPnPNATClass();

            //NATUPNPLib.IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            //NATUPNPLib.IStaticPortMapping map = mappings.Add(Client.P2pPort, "UDP", Client.P2pPort, "192.168.15.7", true, "UDP1");

            //string m = map.Description + map.Enabled + map.ExternalIPAddress + ":" + map.ExternalPort + "+" + map.InternalPort + map.Protocol + "  " + map.ToString();
        }


    }
}
