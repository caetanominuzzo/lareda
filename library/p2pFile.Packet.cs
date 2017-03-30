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

            public string Filename;

            public PacketTypes PacketType;

            public int Offset;

            public IEnumerable<Guid> RequestTraceIdentifier
            {
                get
                {
                    return File.Context.Select(x => x.Request.RequestTraceIdentifier);
                }
            }

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
                    File.Status = FileStatus.dataComplete;

                    File.Success = true;

                    return;
                }

                if (root.Children.All(x => !x.Value.Arrived))
                {
                    File.Status = FileStatus.addressStructureIncomplete;

                    return;
                }

                if (root.Children.Any(x => x.Value.Arrived && x.Value.PacketType != PacketTypes.Addresses))
                {
                    //if (File.FilePackets[0].Arrived &&
                    //    File.FilePackets[File.FilePackets.Count() - 1].Arrived &&
                    //    File.FilePackets[File.FilePackets.Count() - 2].Arrived)
                    if (root.Children.First().Value.Arrived &&
                        root.Children.ElementAt(root.Children.Count() - 1).Value.Arrived &&
                        root.Children.ElementAt(root.Children.Count() - 2).Value.Arrived)
                    {
                        if (root.Children.All(x => x.Value.Arrived))
                        {
                            File.Status = FileStatus.dataComplete;

                            File.Success = true;
                        }
                        else
                            File.Status = FileStatus.dataStructureComplete;
                    }
                    else
                        File.Status = FileStatus.addressStructureComplete;
                }

                if (root.Children.All(x => x.Value.Arrived && x.Value.PacketType == PacketTypes.Addresses
                        && x.Value.Children.All(y => !y.Value.Arrived)))
                {
                    File.Status = FileStatus.addressStructureIncomplete;

                    return;
                }

                if (root.Children.All(x => x.Value.Arrived && x.Value.PacketType == PacketTypes.Addresses
                        && x.Value.Children.Any(y => y.Value.Arrived && y.Value.PacketType != PacketTypes.Addresses)))
                {
                    if (root.Children.First().Value.Children.First().Value.Arrived &&
                        root.Children.Last().Value.Children.Last().Value.Arrived &&
                            ((root.Children.Last().Value.Children.Count() >= 2 && root.Children.Last().Value.Children[root.Children.Last().Value.Children.Count() - 2].Arrived) ||
                            (root.Children.Last().Value.Children.Count() < 2 && root.Children[root.Children.Count() - 2].Children.Last().Value.Arrived)
                            )
                        )
                    {

                        if (root.Children.All(x => x.Value.Children.All(y => y.Value.Arrived)))
                        {
                            File.Status = FileStatus.dataComplete;

                            File.Success = true;
                        }
                        else
                            File.Status = FileStatus.dataStructureComplete;
                    }

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

            SortedList<int, p2pFile.Packet> Children = new SortedList<int, Packet>();

            p2pFile.Packet Parent = null;

            internal Packet(p2pFile file, p2pFile.Packet parent, int offset, byte[] address, string filename = null)
            {
                File = file;

                Address = address;

                Filename = filename;

                Parent = parent;

                Offset = offset;
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
                    PacketType = (PacketTypes)data[0];

                    if (PacketType == PacketTypes.Content)
                        Offset = BitConverter.ToInt32(data, 1);

                    Log.Add(Log.LogTypes.queuePacketArrived, this);

                    this.data = data;

                    File.ReturnRatio = (File.ReturnRatio + (RequestSent / (double)Arrives + 1)) / 2;

                    if (Arrives != 0)
                        return;

                }


                switch (PacketType)
                {
                    case PacketTypes.Addresses:

                        List<byte[]> addresses = Addresses.ToAddresses(data.Skip(pParameters.packetHeaderSize));

                        var count = addresses.Count();

                        var baseOffset = (Parent == null ? 0 : Parent.Offset) * pParameters.packetSize / pParameters.addressSize;

                        baseOffset++;

                        //two items to the main thread
                        ProcessAddPackets(new object[] { addresses.Take(1), baseOffset++ });

                        ProcessAddPackets(new object[] { addresses.Skip(count - 1).Take(2), baseOffset + count - 3 });

                        ProcessAddPackets(new object[] { addresses.Skip(1).Take(count - 3), baseOffset });

                        //ThreadPool.QueueUserWorkItem(ProcessAddPackets, new object[] { addresses.Skip(1).Take(count - 3), baseOffset });

                        //ProcessAddPackets(addresses);

                        break;

                    case PacketTypes.Directory:

                        Directory.CreateDirectory(File.Filename);

                        Dictionary<byte[], string> files =
                            Addresses.ToDirectories(data.Skip(pParameters.packetHeaderSize).ToArray());

                        var offset = 1;

                        foreach (byte[] addr in files.Keys)
                            File.AddPacket(addr, this, offset++, Path.Combine(File.Filename, files[addr]));

                        break;

                    case PacketTypes.Content:

                        var buffer = data.Skip(pParameters.packetHeaderSize).ToArray();

                        if (Offset != BitConverter.ToInt32(data, 1))
                            Offset = BitConverter.ToInt32(data, 1);

                        //Log.Write("OFFSET " + offset.ToString());

                        DelayedWrite.Add(Filename ?? File.Filename, buffer, Offset);

                        break;

                    case PacketTypes.Metapacket:

                        DelayedWrite.Add(Filename, data, 0);

                        break;
                }

                lock (dataArrivedObjectLocker)
                    Arrives++;

                if(this.Offset == 199 || this.Offset == 198)
                { }

                ProcessPacketPriority();

                //File.newPacketEvent.Set();
            }

            private void ProcessAddPackets(object data)
            {
                var datas = (object[])data;

                var addresses = (IEnumerable<byte[]>)datas[0];

                var offset = (int)datas[1];

                if(offset == 198 || offset == 199)
                {

                }

                foreach (byte[] addr in addresses)
                {
                    var p = File.AddPacket(addr, this, offset, Filename);
                    
                    Children.Add(p.Offset, p);

                    offset++;
                }
            }

            internal void Get()
            {
                Log.Add(Log.LogTypes.queueGetPacket, this);


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
