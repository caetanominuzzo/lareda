using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace library
{
    static class Packets
    {
        internal delegate void PacketArrivedHandler(byte[] address, byte[] data);

        internal static event PacketArrivedHandler OnPacketArrived;

        internal static ManualResetEvent localPacketEvent = new ManualResetEvent(true);

        static List<byte[]> packets = new List<byte[]>();

        internal static TimeCounter LocalAddressDistance = new TimeCounter(10, 100);

        internal static TimeCounter LastAccess = new TimeCounter(10, 100);

        static void Load()
        {
            if (!Directory.Exists(pParameters.localPacketsDir))
            {
                Directory.CreateDirectory(pParameters.localPacketsDir);

                return;
            }

            if (!File.Exists(pParameters.localPacketsFile))
            {
                string filename = pParameters.localPacketsFile;

                if (File.Exists(filename + ".new"))
                    File.Move(filename + ".new", filename);
                else if (File.Exists(filename + ".old"))
                    File.Move(filename + ".old", filename);
                else
                    LoadFromPacketsDir();
            }

            var data = File.ReadAllBytes(pParameters.localPacketsFile);

            if (data.Length == 0)
                return;



            var emptyAddress = new byte[pParameters.addressSize];

            if (data.Length < 8)
                return;

            LocalAddressDistance.Add(BitConverter.ToDouble(data, 0));

            if (data.Length < 16)
                return;

            LastAccess.Add(BitConverter.ToDouble(data, 8));

            var offset = 16;

            while (offset < data.Length)
            {
                var buffer = data.Skip(offset).Take(pParameters.addressSize).ToArray();

                offset += pParameters.addressSize;

                if (!Addresses.Equals(emptyAddress, buffer))
                    AddAddress(buffer);
            }
        }

        internal static void Save()
        {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(LocalAddressDistance.Average));

            data.AddRange(BitConverter.GetBytes(LastAccess.Average));

            lock (packets)
            {
                foreach (var a in packets)
                    data.AddRange(a);
            }

            string filename = pParameters.localPacketsFile;

            string newFilename = filename + ".new";

            File.WriteAllBytes(newFilename, data.ToArray());

            if (File.Exists(filename + ".old"))
                File.Delete(filename + ".old");

            if (File.Exists(filename))
                File.Move(filename, filename + ".old");

            File.Move(newFilename, filename);
        }


        private static void LoadFromPacketsDir()
        {
            using (var f = new FileStream(pParameters.localPacketsFile, FileMode.Create, FileAccess.ReadWrite))
            {

                string[] files = Directory.GetFiles(pParameters.localPacketsDir);



                foreach (var file in files)
                {
                    byte[] buffer = Encoding.Unicode.GetBytes(file);

                    f.Write(buffer, 0, buffer.Length);
                }
            }
        }

        static void Remove(byte[] address)
        {
            lock (packets)
                packets.Remove(address);

            string path = Path.Combine(pParameters.localPacketsDir, Utils.ToBase64String(address));

            if (File.Exists(path))
                File.Delete(path);
        }

        static void AddAddress(byte[] address)
        {
            lock (packets)
                packets.Add(address);

            localPacketEvent.Set();
        }

        internal static void Add(byte[] address, byte[] data, Peer peer)
        {
            if (!peer.Equals(Client.LocalPeer))
            {
                byte[] actual = Exists(address);

                VerifyIntegrity(address, actual, data, peer);

                if (actual != null)
                    return;
            }

            DelayedWrite.Add(Path.Combine(pParameters.localPacketsDir, Utils.ToBase64String(address)), data);

            AddAddress(address);

            var packetType = (PacketTypes)data[0];

            if (packetType == PacketTypes.Metapacket && peer == Client.LocalPeer)
            {
                //LocalIndex. AddAddress();    
            }

            if (OnPacketArrived != null)
                OnPacketArrived(address, data);
        }

        internal static byte[] Get(byte[] address)
        {
            string filename = Path.Combine(pParameters.localPacketsDir, Utils.ToBase64String(address));

            byte[] data = DelayedWrite.Get(filename);

            if (data != null)
                return data;
            try
            {
                if (File.Exists(filename))
                    return File.ReadAllBytes(filename);
            }
            catch { }


            return null;
        }

        internal static byte[] Exists(byte[] address)
        {
            for (int i = 0; i < packets.Count(); i++)
            {
                byte[] b = packets[i];

                if (Addresses.Equals(b, address))
                    return b;
            }

            return null;
        }

        static void VerifyIntegrity(byte[] address, byte[] actual, byte[] data, Peer peer)
        {
            //todo:
        }

        #region Thread Refresh

        internal static void Start()
        {
            Load();

            Thread thread = new Thread(Refresh);

            // thread.Start();
        }

        internal static void Stop()
        {
            localPacketEvent.Set();

            Client.Stats.belowMinSentEvent.Set();

            Save();
        }

        static void Refresh()
        {
            while (!Client.Stop)
            {
                bool any;

                lock (packets)
                    any = packets.Any();

                if (!any)
                {
                    localPacketEvent.Reset();

                    localPacketEvent.WaitOne();
                }

                if (Client.Stop)
                    break;

                if (!queue.Any())
                    FillQueue();

                byte[] address = queue.Dequeue();

                Client.Stats.belowMinSentEvent.WaitOne();  //todo: or max confomr % de uso, ver outro uso

                if (Client.Stop)
                    break;

                var total = packets.Count();

                //Probability by total packets
                var pT = (total - pParameters.PacketsMaxItems) / total;

                var avgDistance = LocalAddressDistance.Average;

                //Probability by address distance to Local address
                var pL = Math.Log(Addresses.EuclideanDistance(Client.LocalPeer.Address, address), avgDistance);

                string filename = Path.Combine(pParameters.localPacketsDir, Utils.ToBase64String(address));

                var info = new FileInfo(filename);

                var lastAccess = DateTime.Now.Subtract(info.LastAccessTime).TotalMinutes;

                var avgLastAccess = LastAccess.Average;

                //probability by last access
                var pA = Math.Log(lastAccess, avgLastAccess);

                if (lastAccess < avgLastAccess || pL == 0 || pA == 0 || Utils.Roll(1 / pL * 1 / pA))
                {
                    Sincronize(address);
                }

                if (pT > 0 && Utils.Roll(pT * pL * pA))
                {
                    Remove(address);
                }
            }


        }

        internal static string Print()
        {
            string result = string.Empty;

            foreach (var key in packets)
            {
                var data = Get(key);

                if(data != null)
                    result += string.Format("{0}{1}", Utils.ToBase64String(key), (data.Length < 1024? "<1 kb: " + Encoding.Unicode.GetString(data) : ">1kb"))
                         + Environment.NewLine;
            }

            return result;
        }

        private static void Sincronize(byte[] address)
        {
            var peer = Peers.GetPeer(
               closestToAddress: address,
               excludeOriginAddress: Client.LocalPeer.Address);

            if (peer != null)
            {
                p2pRequest request = new p2pRequest(
                    
                    RequestCommand.Packet, address, Client.LocalPeer,
                    senderPeer: Client.LocalPeer,
                    destinationPeer: peer,
                    data: Get(address));

                request.Enqueue();
            }
        }

        #endregion

        #region Queue

        static Queue<byte[]> queue = new Queue<byte[]>();

        static void FillQueue()
        {
            lock (packets)
                queue = new Queue<byte[]>(packets.OrderBy(x => Utils.Rand.Next()).Take(pParameters.PacketsMaintenanceQueueSize)); ;
        }

        #endregion

    }

    enum PacketTypes
    {
        NonSet = 0,
        Content = 1,
        Addresses = 2,
        Directory = 4,
        Metapacket = 8,
        Index = 16
    }

}
