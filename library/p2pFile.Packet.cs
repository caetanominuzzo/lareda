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

            internal byte[] Address;

            string Filename;

            internal PacketTypes PacketType;

            void ProcessPacketPriority()
            {
                var root = File.Root;

                if (!root.Arrived)
                {
                    File.Status = FileStatus.addressStructureIncomplete;

                    return;
                }

                if (root.Arrived && root.PacketType != PacketTypes.Addresses)
                {
                    File.Status = FileStatus.dataStructureComplete;

                    return;
                }

                if (root.Children.All(x => !x.Arrived))
                {
                    File.Status = FileStatus.addressStructureIncomplete;

                    return;
                }

                if (root.Children.Any(x => x.Arrived && x.PacketType != PacketTypes.Addresses))
                {
                    if (root.Children.First().Arrived &&
                        root.Children.Last().Arrived)
                        File.Status = FileStatus.dataStructureComplete;

                    else
                        File.Status = FileStatus.addressStructureComplete;
                }

                if (root.Children.All(x => x.Arrived && x.PacketType == PacketTypes.Addresses
                        && x.Children.All(y => !y.Arrived)))
                {
                    File.Status = FileStatus.addressStructureIncomplete;

                    return;
                }

                if (root.Children.All(x => x.Arrived && x.PacketType == PacketTypes.Addresses
                        && x.Children.Any(y => y.Arrived && y.PacketType != PacketTypes.Addresses)))
                {
                    if (root.Children.First().Children.First().Arrived &&
                        root.Children.Last().Children.Last().Arrived)
                        File.Status = FileStatus.dataStructureComplete;
                    else
                        File.Status = FileStatus.addressStructureComplete;

                    return;
                }

            }

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

            internal byte[] data { get; private set; }

            object dataArrivedObjectLocker = new object();

            List<p2pFile.Packet> Children = new List<Packet>();

            p2pFile.Packet Parent = null;

            internal Packet(p2pFile file, p2pFile.Packet parent, byte[] address, string filename = null)
            {
                File = file;

                Address = address;

                Filename = filename;
                
                Parent = parent;
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
                    Log.Write("packet arrived:\t[" + Utils.ToSimpleAddress(File.Address) + "]\t[" + Utils.ToSimpleAddress(Address), Log.LogTypes.queueGetPacket);

                    this.data = data;

                    File.ReturnRatio = (File.ReturnRatio + (RequestSent / (double)Arrives + 1)) / 2;

                    if (Arrives != 0)
                        return;

                    PacketType = (PacketTypes)data[0];

                    switch (PacketType)
                    {
                        case PacketTypes.Addresses:

                            List<byte[]> addresses = Addresses.ToAddresses(data.Skip(pParameters.packetHeaderSize));

                            var count = addresses.Count();

                            //if (count > 0)
                            //    Children.Add(File.AddPacket(addresses[0], this, Filename));

                            //if (count > 2)
                            //    Children.Add(File.AddPacket(addresses[count - 2], this, Filename));

                            //if (count > 1)
                            //    Children.Add(File.AddPacket(addresses[count - 1], this, Filename));

                            //if(count > 3)
                            //    Children.AddRange(File.AddPacketRange(addresses.Skip(1).Take(count -3), this, Filename));

                            foreach (byte[] addr in addresses)
                                Children.Add(File.AddPacket(addr, this, Filename));

                            break;

                        case PacketTypes.Directory:

                            Directory.CreateDirectory(File.Filename);

                            Dictionary<byte[], string> files =
                                Addresses.ToDirectories(data.Skip(pParameters.packetHeaderSize).ToArray());

                            foreach (byte[] addr in files.Keys)
                                File.AddPacket(addr, this, Path.Combine(File.Filename, files[addr]));

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

                ProcessPacketPriority();

                File.newPacketEvent.Set();
            }

            internal void Get()
            {
                Log.Write("packet get: \t[" + Utils.ToSimpleAddress(File.Address) + "]\t [" + Utils.ToSimpleAddress(Address) + "]\t " + Filename, Log.LogTypes.queueGetPacket);


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
