using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

//using LumiSoft.Net.Mime.vCard;

namespace library
{
    public partial class p2pFile
    {
        internal class Packet
        {
            p2pFile File;

            internal byte[] Address;

            public string Filename;

            public PacketTypes PacketType;

            public int Offset;

            public int FilePacketOffset;
            
            public IEnumerable<Guid> RequestTraceIdentifier
            {
                get
                {
                    return File.Context.Select(x => x.HttpContext.Request.RequestTraceIdentifier);
                }
            }



            void ProcessPacketPriority()
            {
                var root = File.Root;

                if (this == root && this.PacketType == PacketTypes.Content)
                {
                    File.Status = FileStatus.dataComplete;

                    File.Success = true;

                    return;
                }

                if (File.Levels == 0)
                    File.Levels = 1;

                lock (root.Children)
                {
                    if (!root.Children.Any(x => x.Value.Arrived))
                    {
                        var s = FileStatus.addressStructureIncomplete;

                        File.Status = s;

                        return;
                    }
                }

                lock (root.Children)
                    if (root.Children.Any(x => x.Value.Arrived && x.Value.PacketType == PacketTypes.Content))
                    {
                        if (root.Children.First().Value.Arrived &&
                            root.Children.ElementAt(root.Children.Count() - 1).Value.Arrived &&
                            root.Children.ElementAt(root.Children.Count() - 2).Value.Arrived)
                        {
                            if (root.Children.All(x => x.Value.Arrived))
                            {
                                File.Status = FileStatus.dataComplete;

                                File.Success = true;

                                return;
                            }
                            else
                            {
                                File.Status = FileStatus.dataStructureComplete;

                                return;
                            }
                        }
                        else
                        {
                            File.Status = FileStatus.addressStructureComplete;

                            return;
                        }

                    }

                lock (root.Children)
                    if (root.Children.All(x => x.Value.Arrived && x.Value.PacketType == PacketTypes.Addresses
                        && x.Value.Children.All(y => !y.Value.Arrived)))
                    {
                        File.Status = FileStatus.addressStructureIncomplete;

                        if (File.Levels == 1)
                            File.Levels = 2;

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

            internal SortedList<int, p2pFile.Packet> Children = new SortedList<int, Packet>();

            p2pFile.Packet Parent = null;

            internal Packet(p2pFile file, p2pFile.Packet parent, int filePacketsOffset, int offset, byte[] address, string filename = null)
            {
                File = file;

                Address = address;

                Filename = filename;

                Parent = parent;

                Offset = offset;

                FilePacketOffset = filePacketsOffset;
            }

            internal void VerifyDataArrived(byte[] address, byte[] data)
            {
                if (!Addresses.Equals(Address, address))
                    return;

                Packets.OnPacketArrived -= VerifyDataArrived;

                ProcessDataArrived(data);
            }

            bool isRoot()
            {
                return Parent == null;
            }

            internal int Level
            {
                get { return this.Parent == null ? 0 : this.Parent.Level + 1; }
            }

            IEnumerable<Packet> Parents()
            {
                yield return this;

                if (this.Parent != null)
                    foreach (var p in this.Parent.Parents())
                        yield return p;
            }

            void ProcessDataArrived(byte[] data)
            {
                lock (dataArrivedObjectLocker)
                {
                    PacketType = (PacketTypes)data[0];

                    if (PacketType == PacketTypes.Content)
                    {
                        if (Offset != BitConverter.ToInt32(data, 1))
                            Offset = BitConverter.ToInt32(data, 1);
                    }

                    Log.Add(Log.LogTypes.Queue, Log.LogOperations.Arrived, this);

                    this.data = data;

                    File.ReturnRatio = (File.ReturnRatio + (RequestSent / (double)Arrives + 1)) / 2;

                    if (Arrives != 0)
                        return;

                    Arrives++;
                }


                switch (PacketType)
                {
                    case PacketTypes.Addresses:

                        List<byte[]> addresses = Addresses.ToAddresses(data.Skip(pParameters.packetHeaderSize));

                        var count = addresses.Count();

                        var last_offset = count - 1;

                        var FilePacketsOffset = (Parent == null ? 0 : Parent.Children.Last().Value.FilePacketOffset + this.Offset * (pParameters.packetSize / pParameters.addressSize));
                        
                        FilePacketsOffset++;

                        //first item to the main thread
                        //ProcessAddPackets(new object[] { addresses.Take(1).ToArray(), FilePacketsOffset, 0});
                        ProcessAddPackets(new object[] { data.Skip(pParameters.packetHeaderSize).Take(pParameters.addressSize), FilePacketsOffset, 0 });

                        //second last item to the main thread
                        //ProcessAddPackets(new object[] { addresses.Skip(count - 1).Take(1).ToArray(), FilePacketsOffset + last_offset, last_offset });
                        ProcessAddPackets(new object[] { data.Skip(data.Length - pParameters.addressSize * 2).Take(pParameters.addressSize), FilePacketsOffset + last_offset - 1, last_offset-1 });

                        //last item to the main thread
                        //ProcessAddPackets(new object[] { addresses.Skip(count - 1).Take(1).ToArray(), FilePacketsOffset + last_offset, last_offset });
                        ProcessAddPackets(new object[] { data.Skip( data.Length - pParameters.addressSize).Take(pParameters.addressSize), FilePacketsOffset + last_offset, last_offset });

                        if (Parent == null || Offset == Parent.Children.Keys.Max())
                        {
                            File.GetLastPacket = true;

                            File.GetSecondLastPacket = true;
                        }

                        FilePacketsOffset++;

                        //ThreadPool.QueueUserWorkItem(ProcessAddPackets, new object[] { addresses.Skip(1).Take(count - 2).ToArray(), FilePacketsOffset, 1 });
                        ThreadPool.QueueUserWorkItem(ProcessAddPackets, new object[] {
                            data.Skip(pParameters.packetHeaderSize + pParameters.addressSize).Take(data.Length - pParameters.packetHeaderSize - (pParameters.addressSize * 3)), FilePacketsOffset, 1 });
                        
                        //ProcessAddPackets(new object[] { addresses.Take(count - 1).ToArray(), FilePacketsOffset, 0 });  

                        //ProcessAddPackets(addresses);

                        break;

                    case PacketTypes.Directory:

                        Directory.CreateDirectory(File.Filename);

                        Dictionary<byte[], string> files =
                            Addresses.ToDirectories(data.Skip(pParameters.packetHeaderSize).ToArray());

                        var offset = 1;

                        foreach (byte[] addr in files.Keys)
                            File.AddPacket(addr, this, offset, offset++, Path.Combine(File.Filename, files[addr]));

                        break;

                    case PacketTypes.Content:
                        
                        var buffer = data.Skip(pParameters.packetHeaderSize).ToArray();

                        DelayedWrite.Add(Filename ?? File.Filename, buffer, Offset);

                        lock(File.FilePackets)
                        if (File.Status == FileStatus.dataStructureComplete || this.FilePacketOffset == File.FilePackets.Keys.Max())
                        {
                            File.Status = FileStatus.dataStructureComplete;

                            File.Levels = this.Level;
                        }

                        if (this.Offset == 0 && this.Parents().All(x => x.Offset == 0))
                            File.FirstContentFilePacketOffset = this.FilePacketOffset;

                        lock(File.arrives)
                            File.arrives.Add(this.FilePacketOffset);


                        Client.DownloadComplete(File.Address, File.Filename, File.SpecifFilename, File.Arrives, File.Cursors);

                        break;

                    case PacketTypes.Metapacket:

                        DelayedWrite.Add(Filename, data, 0);

                        break;
                }

                

                ProcessPacketPriority();
                

                File.packetEvent.Set();
            }

            private void ProcessAddPackets(object data)
            {
                var datas = (object[])data;

                var addresses = (IEnumerable<byte>)datas[0];

                var filePacketsOffset = (int)datas[1];

                var baseOffset = (int)datas[2];

                var count = addresses.Count();

                for (var i = 0; i < count; i += pParameters.addressSize)
                //foreach (byte[] addr in addresses)
                {

                    byte[] addr = addresses.Skip(i).Take(pParameters.addressSize).ToArray();

                    var p = File.AddPacket(addr, this, filePacketsOffset, baseOffset, Filename);

                    lock (Children)
                        Children.Add(p.Offset, p);

                    filePacketsOffset++;
                    
                    baseOffset++;
                }
            }

            internal void Get()
            {
                Log.Add(Log.LogTypes.Queue, Log.LogOperations.Get, this);

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
