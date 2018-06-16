//#define COMPLETE

using Frapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TagLib;

namespace library
{
    public partial class p2pFile
    {
        class RecentItem
        {
            internal byte[] Address;

            internal byte[] Property;

            internal byte[] ToConcept;

            internal int CompleteHashCode;

            internal int PropertyHashCode;

            internal byte[] PropertyAddress;
        }

        static Cache<RecentItem> RecentItems = new Cache<RecentItem>(60 * 60 * 1000);

        internal static string[] htmlExtensions = new string[] { ".HTML", ".HTM" };

        internal static void FileUpload(string root_path, string path, byte[] conceptAddress, byte[] userAddress = null, bool newThread = true, List<byte[]> parents = null)
        {
            FileUpload(root_path, new string[] { path }, conceptAddress, userAddress, newThread, parents);
        }

        internal static void FileUpload(string root_path, string[] paths, byte[] conceptAddress, byte[] userAddress = null, bool newThread = true, List<byte[]> parents = null)
        {
            TagLib.File tags = null; 

            try
            {

                if (paths.Length == 1 && !Directory.Exists(paths[0]))
                {
                    try
                    {
                        if (paths[0] != null)
                            tags = TagLib.File.Create(paths[0]);
                    }
                    catch { }

                    var fi = new FileInfo(paths[0]);

                    GeneratePosts(root_path, paths[0], conceptAddress, userAddress, fi.Length + pParameters.packetHeaderSize <= pParameters.packetSize, tags, parents);
                }

                if (newThread)
                {
                    ThreadPool.QueueUserWorkItem(ThreadUpload,
                         //    ThreadUpload(
                         new FileUploadItem
                         {
                             Paths = paths,
                             RootPath = root_path,
                             ConceptAddress = conceptAddress,
                             UserAddress = userAddress,
                             Tags = tags,
                             Parents = parents
                         });
                }
                else
                {
                    ThreadUpload(
                         new FileUploadItem
                         {
                             Paths = paths,
                             RootPath = root_path,
                             ConceptAddress = conceptAddress,
                             UserAddress = userAddress,
                             Tags = tags,
                             Parents = parents
                         });
                }
            }
            finally
            {
                if (tags != null)
                    tags.Dispose();
            }
            //ThreadUpload(  
            //    new FileUploadItem
            //    {
            //        Paths = paths,
            //        ContentAddress = contentAddress,
            //        ConceptAddress = conceptAddress,
            //        UserAddress = userAddress
            //    });
        }

        static void ThreadUpload(object o)
        {


            var file = (FileUploadItem)o;

            if (file.Paths.Length == 1)
            {
                var path = file.Paths[0];

                if (Directory.Exists(path))
                    DirectoryUpload(file.RootPath, file.ConceptAddress, new string[] { path }, file.UserAddress, file.Parents);
                else
                {
                    Client.FileUpload(path, Utils.ToBase64String(file.ConceptAddress));

                    FFMPEG ffmpeg = new FFMPEG();

                    if (file != null &&
                        file.Tags != null &&
                        file.Tags.Properties != null &&
                        file.Tags.Properties.MediaTypes.HasFlag(MediaTypes.Video))
                    {
                        var formats = new[] {
                                        new {
                                            MediaTypes = MediaTypes.Video,
                                            FfmpegSelector = "v",
                                            FileExtension = ".mp4",
                                            MIME_TYPE = VirtualAttributes.MIME_TYPE_VIDEO_STREAM,
                                            Max  = 1 //forgive me
                                        },
                                        new {
                                            MediaTypes = MediaTypes.Audio,
                                            FfmpegSelector = "a",
                                            FileExtension = ".mp4",
                                            MIME_TYPE = VirtualAttributes.MIME_TYPE_AUDIO_STREAM,
                                            Max  = 2// file.Tags.Properties.Codecs.Where(x => x.MediaTypes == MediaTypes.Audio).Count()
                                        },

                                        new {
                                            MediaTypes = MediaTypes.Text,
                                            FfmpegSelector = "s",
                                            FileExtension = ".srt",
                                            MIME_TYPE = VirtualAttributes.MIME_TYPE_TEXT_STREAM,
                                            Max  = file.Tags.Properties.Codecs.Where(x => null != x && x.MediaTypes == MediaTypes.Text).Count()
                                        }};

                        byte[] videoConcept = null;

                        foreach (var format in formats)
                        {
                            var streamNumber = 0;

                            while (streamNumber < format.Max)
                            {
                                if (!Directory.Exists(pParameters.localTempDir))
                                    Directory.CreateDirectory(pParameters.localTempDir);

                                var tmp = Path.Combine(pParameters.localTempDir, Utils.ToBase64String(Utils.GetAddress())) + format.FileExtension;

                                ffmpegProcess.ExecuteAsync(string.Format(@" -ss 00:04:00  -i ""{0}"" -t 00:00:10  -map 0:{1}:{2} -codec copy -y ""{3}""", // >NUL 2>&1 < NUL
                                        file.Paths[0],
                                        format.FfmpegSelector,
                                        streamNumber++,
                                        tmp));

                                //-ss 00:04:00 -i ""{0}"" -t 00:08:10  -map 0:{1}:{2} -codec copy -y ""{3}"""  --with time interval

                                if (format.MediaTypes == MediaTypes.Text)
                                    tmp = Subtitles.ConvertSrtToVtt(tmp);

                                using (var stream = TryNewFileStream(tmp))
                                {
                                    var streamConcept = file.ConceptAddress;

                                    if (videoConcept != null)
                                        streamConcept = videoConcept;

                                    var contentAddress = StreamUpload(
                                                            streamConcept,
                                                            PacketTypes.Content,
                                                            stream, file.Tags,
                                                            format.MIME_TYPE);

                                    var language = VirtualAttributes.PT_BR;

                                    if (streamNumber > 1)
                                        language = VirtualAttributes.EN_US;

                                    var culturetype = Metapacket.Create(contentAddress, language);

                                    Metapacket.Create(culturetype.Address, VirtualAttributes.Culture);

                                    if (videoConcept == null)
                                        videoConcept = contentAddress;
                                }
                            }
                        }

                        var srtFiles = Directory.GetFiles(Path.GetDirectoryName(file.Paths[0]), Path.GetFileNameWithoutExtension(file.Paths[0]) + ".*.srt");

                        foreach (var srtFile in srtFiles)
                        {
                            /////////////////////////////a
                            var tmpSubtitle = Path.Combine(Path.GetDirectoryName(file.Paths[0]), Path.GetFileNameWithoutExtension(file.Paths[0])) + ".*.srt";

                            tmpSubtitle = srtFile;

                            var t = tmpSubtitle.Split('.');

                            var language = VirtualAttributes.PT_BR;

                            if (t[1] == "en")
                                language = VirtualAttributes.EN_US;

                            if (System.IO.File.Exists(tmpSubtitle))
                            {

                                tmpSubtitle = Subtitles.ConvertSrtToVtt(tmpSubtitle);

                                var buffer = System.IO.File.ReadAllText(tmpSubtitle);

                                var buffer1 = System.Text.Encoding.UTF8.GetBytes(buffer);

                                var mem = new MemoryStream(buffer1);

                                var streamConcept = file.ConceptAddress;

                                    if (videoConcept != null)
                                        streamConcept = videoConcept;

                                    var contentAddress = StreamUpload(
                                                            streamConcept,
                                                            PacketTypes.Content,
                                                            mem, file.Tags,
                                                            VirtualAttributes.MIME_TYPE_TEXT_STREAM);

                                    var culturetype = Metapacket.Create(contentAddress, language);

                                    Metapacket.Create(culturetype.Address, VirtualAttributes.Culture);

                                /*
                                using (var stream = TryNewFileStream(tmpSubtitle))
                                {
                                    var streamConcept = file.ConceptAddress;

                                    if (videoConcept != null)
                                        streamConcept = videoConcept;

                                    var contentAddress = StreamUpload(
                                                            streamConcept,
                                                            PacketTypes.Content,
                                                            stream, file.Tags,
                                                            VirtualAttributes.MIME_TYPE_TEXT_STREAM);

                                    var culturetype = Metapacket.Create(contentAddress, language);

                                    Metapacket.Create(culturetype.Address, VirtualAttributes.Culture);
                                }
                                */
                            }
                        }
                        ///////////////////////////



                        var txtFiles = Directory.GetFiles(Path.GetDirectoryName(file.Paths[0]), Path.GetFileNameWithoutExtension(file.Paths[0]) + ".*.txt");

                        foreach (var txtFile in txtFiles)
                        {
                            /////////////////////////////a
                            var tmpSubtitle = Path.Combine(Path.GetDirectoryName(file.Paths[0]), Path.GetFileNameWithoutExtension(file.Paths[0])) + ".*.txt";

                            tmpSubtitle = txtFile;

                            var t = tmpSubtitle.Split('.');

                            var language = VirtualAttributes.PT_BR;

                            if (t[1] == "en")
                                language = VirtualAttributes.EN_US;

                            if (System.IO.File.Exists(tmpSubtitle))
                            {
                                
                                var buffer = System.IO.File.ReadAllText(tmpSubtitle);

                                var buffer1 = System.Text.Encoding.UTF8.GetBytes(buffer);

                                var mem = new MemoryStream(buffer1);

                                var streamConcept = file.ConceptAddress;


                                var contentAddress = StreamUpload(
                                                           streamConcept,
                                                           PacketTypes.Content,
                                                           mem, file.Tags,
                                                           VirtualAttributes.MIME_TYPE_TEXT);

                                var culturetype = Metapacket.Create(contentAddress, language);

                                Metapacket.Create(culturetype.Address, VirtualAttributes.Culture);

                            
                              /*

                                using (var stream = TryNewFileStream(tmpSubtitle))
                                {

                                    var streamConcept = file.ConceptAddress;

                                    var contentAddress = StreamUpload(
                                                            streamConcept,
                                                            PacketTypes.Content,
                                                            stream, file.Tags,
                                                            VirtualAttributes.MIME_TYPE_TEXT);

                                    var culturetype = Metapacket.Create(contentAddress, language);

                                    Metapacket.Create(culturetype.Address, VirtualAttributes.Culture);
                                }
                                */
                              

                            }
                        }
                        ///////////////////////////



                    }
                    else
                    {
                        using (var stream = TryNewFileStream(path))
                        {
                            StreamUpload(file.ConceptAddress, PacketTypes.Content, stream, file.Tags);
                        }
                    }
                }
            }
            else
            {
                var topDirectories = file.Paths.Select(x => Path.GetDirectoryName(x)).Distinct().Count();
                
                if (topDirectories == 1)
                    DirectoryUpload(file.RootPath, file.ConceptAddress, file.Paths, file.UserAddress, file.Parents);
                else
                    foreach (var path in file.Paths)
                        FileUpload(file.RootPath, path, file.ConceptAddress, file.UserAddress, false, file.Parents.ToArray().ToList());
            }
        }

        static void ReadStream(object o)
        {
            var s = (StreamReader)o;

            while (!s.EndOfStream)
            {
                s.ReadToEnd();
            }

        }

        static FileStream TryNewFileStream(string path)
        {
            FileStream result = null;

            var count = 0;

            while (true)
            {
                count++;

                try
                {
                    result = new FileStream(path, FileMode.Open, FileAccess.Read);

                    return result;
                }
                catch (IOException ex)
                {
                    if (count > pParameters.READFILE_RETRY_COUNT)
                        throw ex;
                    else
                        Thread.Sleep(100);
                }
            }
        }


        static void DirectoryUpload(string root_path, byte[] conceptAddress, string[] path, byte[] userAddress, List<byte[]> parents)
        {
            var files = new string[0];

            if (path.Length == 1)
                files = Directory.
                            GetDirectories(path[0], "*", SearchOption.TopDirectoryOnly).
                            Concat(Directory.GetFiles(path[0], "*", SearchOption.TopDirectoryOnly)).
                            ToArray();
            else
                files = path;

            bool web = false;

            var dirName = Path.GetDirectoryName(files[0]);

            GeneratePosts(root_path, dirName, conceptAddress, userAddress, false, null, parents);

            if (null == parents)
                parents = new List<byte[]>();

            parents.Add(conceptAddress);

            foreach (var file in files)
            {
                if (!web && htmlExtensions.Contains(Path.GetExtension(file).ToUpper()))
                {
                    Metapacket.Create(conceptAddress, VirtualAttributes.MIME_TYPE_WEB);

                    web = true;
                }

                var conceptAddress_1 = Utils.GetAddress();
                
                FileUpload(root_path, file, conceptAddress_1, parents: parents.ToArray().ToList());

                //Metapacket.Create(conceptAddress, conceptAddress_1);

                var rootedFileName = file.Substring(path.Length + 1);

                var filename = Encoding.Unicode.GetBytes(rootedFileName);

            }


            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentAddress"></param>
        /// <param name="conceptAddress"></param>
        /// <param name="packetType"></param>
        /// <param name="userAddress"></param>
        /// <param name="path"></param>
        /// <param name="stream"></param>
        /// <returns>Returns true if the content is smaller than the maximum packet size.</returns>
        internal static byte[] StreamUpload(byte[] conceptAddress, PacketTypes packetType, Stream stream, TagLib.File tags = null, byte[] MIME_TYPE = null)
        {
            var contentAddress = Utils.GetAddress();


           

            

            List<byte[]> addresses = new List<byte[]>();

            try
            {
                if (stream.Length == 0)
                    return null;

                while (addresses.Count() != 1)
                {
                    byte[] data = null;

                    int offset = 0;


                    while (offset * pParameters.packetSize < stream.Length) //Parameters.packetSize)//
                    {
                        //if (data == null || data.Length < Parameters.packetSize + Parameters.packetHeaderSize)
                        data = new byte[pParameters.packetSize + pParameters.packetHeaderSize];

                        int bytesRead = stream.Read(data, pParameters.packetHeaderSize, pParameters.packetSize);

                        //int bytesRead = Read(stream, data);

                        data[0] = (byte)packetType;

                        BitConverter.GetBytes(offset).CopyTo(data, 1);

                        //Log.Write("OFFSET " + offset.ToString());

                        offset++;

                        var hash = Utils.ComputeHash(data, pParameters.packetHeaderSize, bytesRead);

                        hash.CopyTo(data, 5); //5 = 1 byte (data type) + 4 byte (offset)

                        byte[] packet_address;

                        if (!addresses.Any() && offset * pParameters.packetSize >= stream.Length)
                        {
                            packet_address = contentAddress;

                            //Conceito/Conteudo - LINK
                            var link = Metapacket.Create(
                                targetAddress: conceptAddress,
                                linkAddress: contentAddress,
                                hashContent: hash);

                            GenerateMime(conceptAddress, stream, tags, MIME_TYPE, link);
                        }
                        else
                            packet_address = Utils.GetAddress();

                        addresses.Add(packet_address.Concat(hash).ToArray());

                        if (bytesRead < pParameters.packetSize)
                            data = data.Take(bytesRead + pParameters.packetHeaderSize).ToArray();

                        Packets.Add(packet_address, data, Client.LocalPeer);
                    }

                    if (addresses.Count() == 1)
                    {
                        //if (path != null)

                        //    GeneratePosts(path, contentAddress, conceptAddress, userAddress, singlePacket);
                    }
                    else if (addresses.Count() > 1)
                    {
                        packetType = PacketTypes.Addresses;

                        List<byte> index = new List<byte>();

                        foreach (byte[] addr in addresses)
                        {
                            var addressSize = pParameters.addressSize + pParameters.hashSize;

                            var packetSize = pParameters.packetSize;

                            if (index.Count() + addressSize > packetSize && packetSize - index.Count() % packetSize < addressSize)
                                index.AddRange(new byte[packetSize - index.Count() % packetSize]);

                            index.AddRange(addr);
                        }

                        addresses.Clear();

                        stream.Close();

                        //data = new byte[index.Count() + Parameters.packetHeaderSize];

                        stream = new MemoryStream(index.ToArray());
                    }
                }

            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return contentAddress;
        }

        private static void GenerateMime(byte[] conceptAddress, Stream stream, TagLib.File tags, byte[] MIME_TYPE, Metapacket link)
        {
            if (MIME_TYPE != null)
                Metapacket.Create(link.Address, MIME_TYPE);

            else if ((tags == null || tags.Properties == null))
            {
                Metapacket.Create(
                        targetAddress: link.Address,
                        linkAddress: VirtualAttributes.MIME_TYPE_DOWNLOAD);
            }
            else if (tags.Properties.MediaTypes == TagLib.MediaTypes.Photo)
            {
                if (stream.Length + pParameters.packetHeaderSize > pParameters.packetSize)
                {
                    GenerateThumb(Image.FromStream(stream), conceptAddress);

                    stream.Position = 0;

                    Metapacket.Create(link.Address, VirtualAttributes.MIME_TYPE_IMAGE);
                }
                else
                    Metapacket.Create(link.Address, VirtualAttributes.MIME_TYPE_IMAGE_THUMB);
            }
            else if (tags.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Audio) || tags.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Video))
            {
                Metapacket.Create(link.Address, VirtualAttributes.MIME_TYPE_VIDEO_STREAM);
            }
        }

        static int Read(Stream stream, byte[] data)
        {
            int offset = 0;

            while (offset < pParameters.packetSize)
            {
                int read = stream.Read(data, pParameters.packetHeaderSize + offset, pParameters.packetSize - offset);

                if (read == 0)
                    break;

                offset += read;
            }

            return offset;
        }

        static Metapacket __createPostTuple(byte[] address, string value, byte[] property, byte[] conceptAddress = null)
        {
            conceptAddress = Client.Post(value, conceptAddress);

            var mValue = Metapacket.Create(address, conceptAddress);

            Metapacket.Create(mValue.Address, property);

            return mValue;
        }


        static Metapacket CreatePostTupleOrReuse(byte[] address, string value, byte[] property, byte[] rootProperty = null)
        {
            if (value == null)
                return null;

            var bValue = Utils.AddressFromBase64String(Utils.ToAddressSizeBase64String(value));

            var completeHashCode = Utils.ComputeChecksum(address.Concat(bValue).Concat(property).ToArray());



            CacheItem<RecentItem> cachedItem = null;

            lock (RecentItems)
                cachedItem = RecentItems.FirstOrDefault(x => x.CachedValue.CompleteHashCode == completeHashCode);

            if (cachedItem != null)
            {
                cachedItem.Reset();

                return null;
            }

            var propertyHashCode = Utils.ComputeChecksum(bValue.Concat(property).ToArray());

            lock (RecentItems)
                cachedItem = RecentItems.FirstOrDefault(x => x.CachedValue.PropertyHashCode == propertyHashCode);

            byte[] valueConceptAddress = null;

            if (cachedItem != null)
            {
                valueConceptAddress = cachedItem.CachedValue.PropertyAddress;

                cachedItem.Reset();
            }
            else
            {
                //hashs from the value to the concenpt
                var hashs = MetaPackets.LocalSearch(bValue, MetaPacketType.Hash);

                //foreach concept
                foreach (var concept in hashs)
                {
                    //pega as propriedades do conceito (em direção ao conteudo, no caso)
                    var conceptProperties = MetaPackets.LocalSearch(concept.LinkAddress, MetaPacketType.Link);

                    foreach (var conceptProperty in conceptProperties)
                    {

                        //sim, proprieda das propriedades do conceito
                        var conceptPropertiesProperties = MetaPackets.LocalSearch(conceptProperty.Address, MetaPacketType.Link);

                        foreach (var conceptPropertiesProperty in conceptPropertiesProperties)
                        {
                            if (Addresses.Equals(conceptPropertiesProperty.LinkAddress, property))
                            {
                                valueConceptAddress = concept.LinkAddress;

                                break;
                            }

                        }

                        if (valueConceptAddress != null)
                            break;
                    }

                    if (valueConceptAddress != null)
                        break;
                }
            }

            var newItem = valueConceptAddress == null;

            if (newItem)
            {
                valueConceptAddress = Client.Post(value);

                if (rootProperty != null)
                    Metapacket.Create(valueConceptAddress, rootProperty);
            }

            var mValue = Metapacket.Create(address, valueConceptAddress);

            Metapacket.Create(mValue.Address, property);

            if (newItem || cachedItem != null)
            {
                lock (RecentItems)
                    RecentItems.Add(
                        new RecentItem()
                        {
                            CompleteHashCode = completeHashCode,
                            PropertyHashCode = propertyHashCode,
                            Address = bValue,
                            Property = property,
                            PropertyAddress = valueConceptAddress,
                            ToConcept = address
                        });
            }

            if (newItem)
                return mValue;
            else
                return null;
        }

        static byte[] GeneratePosts(string root_path, string path, byte[] conceptAddress, byte[] userAddress, bool singlePacketFile, TagLib.File tags, List<byte[]> parents = null)
        {
            Metapacket.Create(conceptAddress, VirtualAttributes.CONCEITO);

            if (userAddress != null)
            {
                var user = Metapacket.Create(
                   targetAddress: conceptAddress,
                   linkAddress: userAddress);

                Metapacket.Create(
                    targetAddress: user.Address,
                    linkAddress: VirtualAttributes.AUTHOR);
            }

            byte[] last_parent = null;

            byte[] first_parent = null;

            Metapacket last_parent_link = null;

            if(parents != null)
            {
                last_parent = parents.Last();

                first_parent = parents.First();

                foreach (var parent in parents)
                {
                    var parent_link = Metapacket.Create(targetAddress: parent, linkAddress: conceptAddress);

                    if (parent == last_parent)
                        last_parent_link = parent_link;
                }
            }

            if (path != null && Directory.Exists(path))
            {
                var root_type = System.IO.File.Exists(Path.Combine(path, "index.html")) ? VirtualAttributes.ROOT_APP : VirtualAttributes.ROOT_SEQUENCE;

                var link = Metapacket.Create(targetAddress: conceptAddress, linkAddress: root_type);

                Metapacket.Create(link.Address, VirtualAttributes.ROOT_TYPE);

                //yep, Path.GetFilename to get the name of the directory 
                var dirname = Path.GetFileName(path);

                int i = -1;

                //if (int.TryParse(dirname, out i))
                //    //    Metapacket.Create(link.Address, MIME_TYPE);
                //    CreatePostTupleOrReuse(conceptAddress, i.ToString("n2"), VirtualAttributes.ORDER);

                Client.Post(dirname, conceptAddress);

                return conceptAddress;
            }

            Dictionary<string, string> nameItems = null;

            try
            {
                if (path != null)
                {
                    var title = Path.GetFileNameWithoutExtension(path);

                    nameItems = TorrentNameParser.ParseTitle(title);
                }
            }
            catch (Exception e)
            {

            }

            if (null != first_parent)
            {
                var index_name = path.Replace(root_path, "").Replace("\\", "/");

                if(index_name.Contains("index"))
                {

                }

                var index = Utils.ToAddressSizeArray(Utils.ToBase64String(first_parent) + index_name);

                var sindex = Utils.ToBase64String(index);

                var indexConcept = Metapacket.Create(
                    targetAddress: index,
                    linkAddress: conceptAddress,
                    type: MetaPacketType.Hash);
            }

            if (nameItems.ContainsKey("episode"))
            {
                var ep = nameItems["episode"];

                int i = -1;

                //if (int.TryParse(ep, out i))
                //    CreatePostTupleOrReuse(conceptAddress, i.ToString("n2"), VirtualAttributes.ORDER);
            }

            if (tags == null || tags.Properties == null)
            {
                if (path != null)
                {
                    var title = Path.GetFileNameWithoutExtension(path);

                    if (nameItems.ContainsKey("title"))
                        title = nameItems["title"];

                    Client.Post(title, conceptAddress);
                }

                return conceptAddress;
            }

            var mediaType = p2pRequest.bytes_empty;

            byte[] rootProperty = null;

            if (tags.Properties.MediaTypes == TagLib.MediaTypes.Photo)
            {
                rootProperty = VirtualAttributes.ROOT_IMAGE;

            }
            else if (tags.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Audio) || tags.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Video))
            {
                rootProperty = VirtualAttributes.ROOT_STREAM;
            }

            if (rootProperty != null)
            {
                var m = Metapacket.Create(conceptAddress, rootProperty);

                Metapacket.Create(m.Address, VirtualAttributes.ROOT_TYPE);
            }

            if (!string.IsNullOrWhiteSpace(tags.Tag.Title))
            {
                Client.Post(tags.Tag.Title, conceptAddress);

            }
            else if (nameItems == null)
            {
                Client.Post(Path.GetFileNameWithoutExtension(path), conceptAddress);
            }

            if (nameItems != null)
            {
                if (nameItems.ContainsKey("title"))
                    Client.Post(nameItems["title"], conceptAddress);

#if COMPLETE

                Metapacket show = null;

                if (nameItems.ContainsKey("show"))
                    show = CreatePostTupleOrReuse(conceptAddress, nameItems["show"], VirtualAttributes.Show, rootProperty);

                if (show != null)
                {
                    //try
                    //{
                    //    GimageSearchClient client = new GimageSearchClient("http://www.google.com");
                    //    IList<IImageResult> results;

                    //    IAsyncResult result = client.BeginSearch(
                    //       keyword: "\"" + nameItems["show"] + " show\" background",  //param1
                    //       resultCount: 1,
                    //       imageSize: "large",
                    //       colorization: string.Empty,
                    //       imageType: string.Empty,
                    //       fileType: string.Empty,
                    //       state: null,
                    //       callback:
                    //           ((arResult) => //param3
                    //           {
                    //               results = client.EndSearch(arResult);

                    //               WebClient c = new WebClient();

                    //               var d = c.DownloadData(results[0].Url);

                    //               var image = Image.FromStream(new MemoryStream(d));

                    //               GenerateThumb(image, show.LinkAddress);

                    //               //Client.PostImage(show.LinkAddress, d);

                    //               var i = 0;
                    //               //var x = 
                    //           }));
                    //}
                    //catch (Exception ex)
                    //{
                    //}
                }

                if (nameItems.ContainsKey("season"))
                {

                    CreatePostTupleOrReuse(conceptAddress, nameItems["season"], VirtualAttributes.Season);
                    //var season = CreatePostTupleOrReuse(conceptAddress, nameItems["season"], VirtualAttributes.Season, rootProperty);

                    //if (season != null)
                    //{
                    //    if (show != null)
                    //        CreatePostTupleOrReuse(show.LinkAddress, nameItems["season"], VirtualAttributes.Season, rootProperty);
                    //}



                }



                if (nameItems.ContainsKey("episode"))
                    CreatePostTupleOrReuse(conceptAddress, nameItems["episode"], VirtualAttributes.Episode);


                if (nameItems.ContainsKey("year"))
                    CreatePostTupleOrReuse(conceptAddress, nameItems["year"], VirtualAttributes.Year);

                if (nameItems.ContainsKey("resolution"))
                    CreatePostTupleOrReuse(conceptAddress, nameItems["resolution"], VirtualAttributes.Resolution);

                if (nameItems.ContainsKey("quality"))
                    CreatePostTupleOrReuse(conceptAddress, nameItems["quality"], VirtualAttributes.Quality);

                if (nameItems.ContainsKey("codec"))
                    CreatePostTupleOrReuse(conceptAddress, nameItems["codec"], VirtualAttributes.VideoCodec);

                if (nameItems.ContainsKey("audio"))
                    CreatePostTupleOrReuse(conceptAddress, nameItems["audio"], VirtualAttributes.AudioCodec);

#endif

            }

#if COMPLETE

            if (tags.Tag.Performers.Any())
                foreach (var item in tags.Tag.Performers)
                    CreatePostTupleOrReuse(conceptAddress, item, VirtualAttributes.Artist, rootProperty);

            else if (tags.Tag.AlbumArtists.Any())
                foreach (var item in tags.Tag.AlbumArtists)
                    CreatePostTupleOrReuse(conceptAddress, item, VirtualAttributes.Artist, rootProperty);

            foreach (var item in tags.Tag.Composers)
                CreatePostTupleOrReuse(conceptAddress, item, VirtualAttributes.Artist);


            if (!string.IsNullOrWhiteSpace(tags.Tag.Conductor))
                CreatePostTupleOrReuse(conceptAddress, tags.Tag.Conductor, VirtualAttributes.Artist);


            if (!string.IsNullOrWhiteSpace(tags.Tag.Comment))
                CreatePostTupleOrReuse(conceptAddress, tags.Tag.Comment, VirtualAttributes.Comment);


            foreach (var item in tags.Tag.Genres)
                CreatePostTupleOrReuse(conceptAddress, item, VirtualAttributes.Genre, rootProperty);


            if (tags.Tag.Track > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Tag.Track.ToString(), VirtualAttributes.Track);


            if (!string.IsNullOrWhiteSpace(tags.Tag.Lyrics))
                CreatePostTupleOrReuse(conceptAddress, tags.Tag.Lyrics, VirtualAttributes.Lyrics);


            if (!string.IsNullOrWhiteSpace(tags.Tag.Grouping))
                CreatePostTupleOrReuse(conceptAddress, tags.Tag.Grouping, VirtualAttributes.Grouping);


            if (tags.Tag.BeatsPerMinute > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Tag.BeatsPerMinute.ToString(), VirtualAttributes.BeatsPerMinute);


            if (tags.Tag.Disc > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Tag.Disc.ToString(), VirtualAttributes.Disc);


            if (tags.Properties.Duration.TotalMilliseconds > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.Duration.ToString(@"hh\:mm\:ss"), VirtualAttributes.Duration);

            if (!string.IsNullOrWhiteSpace(tags.Properties.Description))
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.Description, VirtualAttributes.Description);

            if (tags.Properties.AudioBitrate > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.AudioBitrate.ToString(), VirtualAttributes.AudioBitrate);

            if (tags.Properties.AudioSampleRate > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.AudioSampleRate.ToString(), VirtualAttributes.AudioSampleRate);


            if (tags.Properties.BitsPerSample > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.BitsPerSample.ToString(), VirtualAttributes.BitsPerSample);


            if (tags.Properties.AudioChannels > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.AudioChannels.ToString(), VirtualAttributes.AudioChannels);


            if (tags.Properties.PhotoQuality > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.PhotoQuality.ToString(), VirtualAttributes.Quality);


            if (tags.Properties.VideoHeight > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.VideoHeight.ToString(), VirtualAttributes.Height);
            else if (tags.Properties.PhotoHeight > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.PhotoHeight.ToString(), VirtualAttributes.Height);

            if (tags.Properties.VideoWidth > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.VideoWidth.ToString(), VirtualAttributes.Width);
            else if (tags.Properties.PhotoWidth > 0)
                CreatePostTupleOrReuse(conceptAddress, tags.Properties.PhotoWidth.ToString(), VirtualAttributes.Width);


#endif


            //if (f.GetType().BaseType == typeof(TagLib.Image.ImageBlockFile))
            //{
            //    GenerateThumb(Image.FromFile(path), null, address);

            //    return;
            //}

            if (string.IsNullOrWhiteSpace(tags.Tag.Album))
            {
                //if (f.Tag.Year > 0)
                //    CreatePostTupleOrReuse(conceptAddress, f.Tag.Year.ToString(), VirtualAttributes.Year);


                GenearatePictures(conceptAddress, path, tags);


            }
            else
            {
                var album = CreatePostTupleOrReuse(conceptAddress, tags.Tag.Album, VirtualAttributes.Album, rootProperty);

                if (album != null)
                {
                    if (tags.Tag.Performers.Any())
                        foreach (var item in tags.Tag.Performers)
                            CreatePostTupleOrReuse(album.LinkAddress, item, VirtualAttributes.Artist, rootProperty);

                    if (tags.Tag.AlbumArtists.Any())
                        foreach (var item in tags.Tag.AlbumArtists)
                            CreatePostTupleOrReuse(album.LinkAddress, item, VirtualAttributes.Artist, rootProperty);

                    //if (f.Tag.Year > 0)
                    //    CreatePostTupleOrReuse(album.LinkAddress, f.Tag.Year.ToString(), VirtualAttributes.Year);

                    GenearatePictures(album.LinkAddress, path, tags);

                }
            }

            return conceptAddress;
        }

        static void GenearatePictures(byte[] address, string path, TagLib.File file)
        {
            if (file.Tag.Pictures.Any())
            {
                foreach (var item in file.Tag.Pictures)
                    Client.PostImage(address, item.Data.Data);

                return;
            }

            Stream stream = null;

            try
            {

                stream = Win32ImageFactory.ExtractThumbnail(path);

                if (stream != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);

                        Client.PostImage(address, ms.ToArray());
                    }
                }
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

        }

        //private static void CreatePicturePost(byte[] address, Stream pic)
        //{
        //    var buffer = new byte[pic.Length];

        //    pic.Read(buffer, 0, buffer.Length);

        //    byte[] pictureAddress = null;

        //    var crc = Utils.ComputeCRC(buffer);


        //    //todo:seaerch by crc

        //    if (pictureAddress != null)
        //    {
        //        Metapacket.Create(address, pictureAddress);

        //        return;
        //    }

        //    pictureAddress = Utils.GetAddress();



        //    pic.Seek(0, 0);

        //    //GenerateThumb(Image.FromStream(pic), null, pictureAddress);

        //    pic.Seek(0, 0);

        //    Upload(pictureAddress, PacketTypes.Content, null, null, pic);



        //    Metapacket.Create(pictureAddress, VirtualAttributes.CONCEITO);

        //    var link = Metapacket.Create(address, pictureAddress);

        //    Metapacket.Create(link.Address, VirtualAttributes.MIME_TYPE_IMAGE);
        //}

        static void GenerateThumb(Image image, byte[] address)
        {
            var result = (Image)new Bitmap(image, new Size(280, 170));

            var stream = result.ToStream(ImageFormat.Jpeg);

            //var data = new byte[stream.Length];

            // stream.Read(data, 0, data.Length);

            StreamUpload(null, PacketTypes.Content, stream, null, VirtualAttributes.MIME_TYPE_IMAGE_THUMB);



        }

        class FileUploadItem
        {
            internal byte[] ConceptAddress;

            internal byte[] UserAddress;

            internal string[] Paths;

            internal string RootPath;

            internal TagLib.File Tags;

            internal List<byte[]> Parents = null;
        }

    }
}
