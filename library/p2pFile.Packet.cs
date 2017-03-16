using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

//using LumiSoft.Net.Mime.vCard;

namespace library
{
    partial class p2pFile
    {
        internal class Packet
        {
            p2pFile File;

            byte[] Address;

            string Filename;

            internal bool Arrived
            {
                get
                {
                    lock (dataArrivedObjectLocker)
                        return Arrives > 0;
                }
            }

            int Arrives;

            internal int RequestSent;

            internal bool MayHaveLocalData = true;

            object dataArrivedObjectLocker = new object();

            List<p2pFile.Packet> Children = new List<Packet>();

            internal Packet(p2pFile file, byte[] address, string filename = null)
            {
                File = file;

                Address = address;

                Filename = filename;
            }

            internal void VerifyDataArrived(byte[] address, byte[] data)
            {
                if (!Addresses.Equals(Address, address))
                    return;

                Packets.OnPacketArrived -= VerifyDataArrived;

                ProcessDataArrived(data);
            }

            void ProcessDataArrived(byte[] data)
            {
                lock (dataArrivedObjectLocker)
                {


                    File.ReturnRatio = (File.ReturnRatio + (RequestSent / (double)Arrives + 1)) / 2;

                    if (Arrives != 0)
                        return;


                    var packetType = (PacketTypes)data[0];

                    switch (packetType)
                    {
                        case PacketTypes.Addresses:

                            List<byte[]> addresses = Addresses.ToAddresses(data.Skip(pParameters.packetHeaderSize));

                            foreach (byte[] addr in addresses)
                                Children.Add(File.AddPacket(addr, Filename));

                            break;

                        case PacketTypes.Directory:

                            Directory.CreateDirectory(File.Filename);

                            Dictionary<byte[], string> files =
                                Addresses.ToDirectories(data.Skip(pParameters.packetHeaderSize).ToArray());

                            foreach (byte[] addr in files.Keys)
                                File.AddPacket(addr, Path.Combine(File.Filename, files[addr]));

                            break;

                        case PacketTypes.Content:

                            var buffer = data.Skip(pParameters.packetHeaderSize).ToArray();

                            var offset = BitConverter.ToInt32(data, 1);

                            //Log.Write("OFFSET " + offset.ToString());

                            DelayedWrite.Add(Filename ?? File.Filename, buffer, offset);

                            break;

                        case PacketTypes.Metapacket:

                            DelayedWrite.Add(Filename, data, 0);

                            break;
                    }

                    Arrives++;
                }
            }

            internal void Get()
            {
                Log.Write("packet get: " + Utils.ToSimpleAddress(Address) + "\t" + Utils.ToBase64String(Address));

                byte[] data = null;

                if (this.Filename.StartsWith("cache/", StringComparison.CurrentCultureIgnoreCase) || MayHaveLocalData)
                {
                    data = Packets.Get(Address);

                    if (data == null)
                        MayHaveLocalData = false;
                }

                if (data == null)
                {
                    var search = MetaPackets.LocalSearch(Address, MetaPacketType.Link);

                    if (search.Any(x => Addresses.Equals(x.LinkAddress, VirtualAttributes.MIME_TYPE_DOWNLOAD)))
                    {
                        GetDirectory(search);

                        return;
                    }
                    else
                    {
                        Client.Stats.PresumedReceived.Add((int)(pParameters.packetSize / File.ReturnRatio));

                        Client.Stats.belowMaxReceivedEvent.WaitOne(File.MayHaveLocalData() ? 0 : Timeout.Infinite);

                        var peer = Peers.GetPeer(Address);

                        if (peer == null)
                            return;

                        //todo: IMPORTANT! Refazer: removido parametro wait. Se não tiver chance de ter localdata (!File.MayHaveLocalData())  fazer Peers.idlePeerEvent.WaitOne

                        new p2pRequest(RequestCommand.Packet, Address, data: Address).Enqueue();

                        //var sent = p2pRequest.Send(
                        //    address: Address,
                        //    wait: !File.MayHaveLocalData());
                        
                        var sent = true;

                        if (sent)
                        {
                            RequestSent++;

                            if (RequestSent == 1)
                                Packets.OnPacketArrived += VerifyDataArrived;
                        }
                    }
                }
                else if (data != null)
                    ProcessDataArrived(data);
            }

            private void GetDirectory(IEnumerable<Metapacket> search)
            {
                //todo:refazer

                //Directory.CreateDirectory(Filename);

                //search.ToList().ForEach(x =>
                //{
                //    var refs = MetaPackets.Search(x.Address);

                //    refs.ToList().ForEach(b =>
                //    {
                //        if (b.LinkAddress == null)
                //            return;

                //        var file = MetaPackets.Search(b.LinkAddress);

                //        var download = file.FirstOrDefault(a => Addresses.Equals(a.LinkAddress, VirtualAttributes.MIME_TYPE_DOWNLOAD));

                //        if (download != null)
                //        {
                //            var name = string.Empty;

                //            var extension = string.Empty;

                //            file.ToList().ForEach(z =>
                //            {
                //                var posts = MetaPackets.Search(z.Address);

                //                if (posts.Any(w => w.Content == Utils.ToAddressSizeBase64String("Name")))
                //                {
                //                    name = z.Content;

                //                    return;
                //                }

                //                if (posts.Any(w => w.Content == Utils.ToAddressSizeBase64String("Extension")))
                //                {
                //                    extension = z.Content;

                //                    return;
                //                }

                //            });

                //            if(string.IsNullOrEmpty(File.SpecifFilename) || (name + extension).Equals(File.SpecifFilename))
                //                File.AddPacket(download.TargetAddress, Path.Combine(File.Filename ?? Filename, name + extension));
                //        }
                //    });


                //});

                //Arrives++;
            }
        }
    }
}
