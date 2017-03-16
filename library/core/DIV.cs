using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class DIV
    {
        internal string simpleAddress
        {
            get { return Utils.ToSimpleAddress(Address); }
        }

        internal byte[] Address = new byte[pParameters.addressSize];

        internal byte[] Hash = null;

        internal Metapacket Src = null;

        internal List<DIV> Children = new List<DIV>();

        internal List<DIV> Parents = new List<DIV>();

        internal double Weight = 1;

        internal double RelativeWeight = 1;

        internal double AverageChildrenWeight = 1;

        internal bool IsValid = false;

        bool isRendered = false;

        internal bool IsRendered
        {
            get { return isRendered; } // IsVirtualAttribute || 
            set { isRendered = value; }
        }

        bool index = false;

        internal bool Index
        {
            get { return index; } //
            set { index = value; }
        }

        internal bool IsVirtualAttribute = false;

        internal bool IsReseted = false;

        internal Dictionary<byte[], int> Distances = new Dictionary<byte[], int>(new ByteArrayComparer());


        internal double UncollapsedWeight(DIV parent)
        {
            if (Src == null)
                return Weight;

            var target = Find(this.Children, Src.TargetAddress);

            var link = Find(this.Children, Src.LinkAddress);

            if (target == parent)
                return link.Weight;

            if (link == parent)
                return target.Weight;

            return Weight;
        }

        internal double CollapsedWeight(DIV parent)
        {
            //if (Distances[(int)DIV.DISTANCE_MARKERS.Concept] == 0 && Distances[(int)DIV.DISTANCE_MARKERS.Content] == 0)
            //    return Weight;
            //else
            {
                if (Src == null)
                    return Weight;

                var target = Find(this.Children, Src.TargetAddress);

                var link = Find(this.Children, Src.LinkAddress);

                if (target == parent)
                    return link.Weight;

                if (link == parent)
                    return target.Weight;

                return Weight;

                //if (Children.Any())
                //    return Children.Max(x => x.Weight);

                //return Weight;

                var ttt = getChildren(parentCount: 1, not: parent);

                return ttt.Any() ? ttt.First().Weight : Weight;
                //return Children.Any() ? Children.Max(x => x.Weight) : Weight;
            }
        }




        internal static DIV Find(List<DIV> RootResults, byte[] address)
        {
            foreach (var c in RootResults)
                if (Addresses.Equals(c.Address, address))
                    return c;

            return null;
        }

        IEnumerable<DIV> FindAll(byte[] address, Queue<byte[]> parents = null)
        {
            //Log.Write("..." + Utils.ToBase64String(address));

            List<DIV> result = new List<DIV>();

            if (parents == null)
                parents = new Queue<byte[]>();

            //
            if (parents.Count() < 2 && !parents.Any(x => Addresses.Equals(x, this.Address)))
            {

                if (Addresses.Equals(this.Address, address))
                    result.Add(this);

                else
                {
                    parents.Enqueue(this.Address);

                    lock (this)
                        foreach (var child in Children)
                        {
                            result.AddRange(child.FindAll(address, parents));

                            //if (result != null)
                            //{
                            //    foreach (var r in result)
                            //        yield return r;
                            //}
                        }

                    parents.Dequeue();
                }

            }

            return result;
        }

        bool FindAny(byte[] address)
        {
            if (Addresses.Equals(this.Address, address))
                return true;

            lock (this)
                foreach (var child in Children)
                {
                    if (child.FindAny(address))
                        return true;
                }

            return false;
        }

        public string ToString()
        {
            return Utils.ToSimpleAddress(Utils.ToSimpleName(Address));
        }

        public string Print(List<DIV> done = null)
        {
            if (done == null)
                done = new List<DIV>();
            else if (done.Any(x => Addresses.Equals(x.Address, Address)))
                return string.Empty;

            done.Add(this);

            var result = Utils.ToSimpleAddress(Address) + ";\r\n";

            if (result[0] == ';')
            {

            }

            foreach (var c in Children)
            {
                result += (Utils.ToSimpleAddress(Address) + " -> " + Utils.ToSimpleAddress(c.Address) + ";\r\n");

                var s = c.Print(done);

                if (!string.IsNullOrEmpty(s))
                {
                    result += s;
                }
            }

            return result;

        }


        public string Serialize(int parentCount = 0, GettingChildrenFilter childrenFilter = GettingChildrenFilter.Inferior)
        {
            if (IsRendered)
                return string.Empty;

            if (this.IsVirtualAttribute)
                return string.Empty;

            var maxDeepness = 1;// Client.MaxDeepness;

            if (childrenFilter == GettingChildrenFilter.Superior)
                maxDeepness = 3;



            if (parentCount > maxDeepness)
                return string.Empty;

           

            IsRendered = true;
            
            if (SearchResult.GetDeepDistance(this, VirtualAttributes.CONCEITO) == 0 && 
                SearchResult.GetDeepDistance(this, VirtualAttributes.CONTEUDO) == 0)
            {
                if (childrenFilter == GettingChildrenFilter.Inferior)
                    parentCount++;

                string address = "\"address\": \"" + Utils.ToBase64String(this.Address) + "\"";

                string index = "\"index\": \"0\"";

                string date = this.Src == null ? string.Empty : "\"date\": \"" + this.Src.Creation.ToUniversalTime().ToLongDateString() + "\"";


                #region AUTHOR

                var firstauthor = SearchResult.ClosestMarker(this, VirtualAttributes.AUTHOR);

                var author = string.Empty;

                if (firstauthor != null)
                {
                    if (!Addresses.Equals(this.Address, firstauthor.Address))
                    {
                        var a = firstauthor.Serialize(parentCount);

                        if (!string.IsNullOrWhiteSpace(a))
                            author = "\"author\": " + a + "";
                    }
                }

                #endregion

                #region PIC

                var pic = string.Empty;

                var picAddress = SearchResult.FirstContent(this, VirtualAttributes.MIME_TYPE_IMAGE_THUMB);

                var videoStream = string.Empty;


                var subtitleStreamList = string.Empty;

                var audioStreamList = string.Empty;

                var audioStream = string.Empty;
                var subtitleStream = string.Empty;
                var download = string.Empty;

                pic = "\"pic\": \"" + picAddress + "\"";


                if (parentCount <= 1)
                {
                    var videoDiv = SearchResult.ClosestMarker(this, VirtualAttributes.MIME_TYPE_VIDEO_STREAM);

                    var videoStreamAddres = videoDiv == null ? string.Empty : Utils.ToBase64String(videoDiv.Address);

                    videoStream = "\"video\": \"" + videoStreamAddres + "\"";

                    if (picAddress.Length == 0 && videoStreamAddres.Length == 0 && firstauthor != null)
                        picAddress = SearchResult.FirstContent(firstauthor, VirtualAttributes.MIME_TYPE_IMAGE_THUMB);

                    pic = "\"pic\": \"" + picAddress + "\"";

                    audioStream = string.Empty;

                    var audioStreamAddres = SearchResult.FirstContent(videoDiv == null ? this : videoDiv, VirtualAttributes.MIME_TYPE_AUDIO_STREAM);

                    audioStream = "\"audio\": \"" + audioStreamAddres + "\"";

                    subtitleStream = string.Empty;

                    var subtitleStreamAddres = SearchResult.FirstContent(videoDiv == null ? this : videoDiv, VirtualAttributes.MIME_TYPE_TEXT_STREAM);

                    subtitleStream = "\"subtitle\": \"" + subtitleStreamAddres + "\"";
                    
                    subtitleStreamList = string.Format("\"subtitles\": [{0}]", SerializeChildren(videoDiv, VirtualAttributes.MIME_TYPE_TEXT_STREAM));

                    audioStreamList = string.Format("\"audios\": [{0}]", SerializeChildren(videoDiv, VirtualAttributes.MIME_TYPE_AUDIO_STREAM));

                    #endregion



                    download = string.Empty;

                    var downloadAddress = SearchResult.FirstContent(this, VirtualAttributes.MIME_TYPE_DOWNLOAD);

                    download = "\"download\": \"" + downloadAddress + "\"";

                }

                var text = SearchResult.FirstContent(this, VirtualAttributes.MIME_TYPE_TEXT, true);

                text = "\"text\": \"" + text + "\"";

                var thumb_text = SearchResult.FirstContent(this, VirtualAttributes.MIME_TYPE_TEXT_THUMB, true);

                thumb_text = "\"thumb_text\": \"" + thumb_text + "\"";

                var weight = "\"weight\": \"" + this.Weight.ToString("n2") + "\"";


                var simpleAddressAttr = "\"simple\": \"" + Utils.ToSimpleAddress(this.Address) + "\"";

                var average = "\"average\": \"" + this.AverageChildrenWeight.ToString("n2") + "\"";

                var collapsed = "\"collapsed\": \"" + CollapsedWeight(this).ToString("n2") + "\"";


                var inferiorChildren = string.Empty;

                var superiorChildren = string.Empty;

                if (childrenFilter == GettingChildrenFilter.Inferior)
                {
                    var inferiorChildrenList = SerializeChildren(parentCount, true, GettingChildrenFilter.Inferior);

                    if (inferiorChildrenList.Any())
                        inferiorChildren = string.Format("\"children\": [{0}]", string.Join(", ", inferiorChildrenList));
                }

                if (this.Index)
                {
                    var superiorChildrenList = SerializeChildren(parentCount, true, GettingChildrenFilter.Superior);

                    if (superiorChildrenList.Any())
                        superiorChildren = string.Format("\"superiorchildren\": [{0}]", string.Join(", ", superiorChildrenList));
                }

                var results = new string[] { audioStreamList, subtitleStreamList, thumb_text, address, index, weight, date, text, pic, videoStream, audioStream, subtitleStream, author, download, inferiorChildren, superiorChildren }.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                if (results.Any())
                {
                    results.Add(simpleAddressAttr);

                    results.Insert(0, average);

                    results.Insert(0, collapsed);

                    results.Insert(0, "\"root\": \"post\"");

                    //results.Add(average);

                    return string.Format("{{{0}}}", string.Join(", ", results));
                }
                else
                    return string.Empty;
            }
            else
            {
                if (true)
                {
                    var inferiorChildrenList = SerializeChildren(parentCount, false, childrenFilter);

                    if (inferiorChildrenList.Any())
                        return string.Join(", ", inferiorChildrenList);
                }
                return string.Empty;
            }

        }

        private string SerializeChildren(DIV item, byte[] marker)
        {
            var DIVSubtitles = SearchResult.ClosestMarkerList(item == null ? this : item, marker);

            var legendaResultAddress = new List<string>();

            foreach (var DIVSubtitle in DIVSubtitles)
            {
                if (DIVSubtitle == null)
                    continue;

                DIV validChildren = DivPropertiesList(DIVSubtitle).FirstOrDefault();



                if (validChildren != null)
                {
                    var xxx = SearchResult.FirstContent(validChildren, VirtualAttributes.MIME_TYPE_TEXT_THUMB, true);

                    var linkDoValidChildren = DIV.Find(validChildren.Children, validChildren.Src.LinkAddress);

                    if (linkDoValidChildren != null)
                    {
                        var xx = SearchResult.FirstContent(linkDoValidChildren, VirtualAttributes.MIME_TYPE_TEXT_THUMB, true);

                        legendaResultAddress.Add(string.Format("{{\"thumb_text\": \"{0}\", \"address\": \"{1}\"}}", xx, Utils.ToBase64String(DIVSubtitle.Address)));
                    }
                }
            }

            return string.Join(", ", legendaResultAddress);
        }


        //Properties are any children which are not from the source metapacket
        private static IEnumerable<DIV> DivPropertiesList(DIV legenda)
        {
            var linkDaLegenda = DIV.Find(legenda.Children, legenda.Src.LinkAddress);

            var targetDaLegenda = DIV.Find(legenda.Children, legenda.Src.TargetAddress);

            var addressDaLegenda = DIV.Find(legenda.Children, legenda.Src.Address);

            foreach (var filhoDoLinkDaLegenda in legenda.Children)
            {
                if (filhoDoLinkDaLegenda == linkDaLegenda || filhoDoLinkDaLegenda == legenda || filhoDoLinkDaLegenda == addressDaLegenda)
                    continue;

                yield return filhoDoLinkDaLegenda;

                break;
            }
        }

        private string[] SerializeChildren(int parentCount, bool onChildren, GettingChildrenFilter childrenFilter)
        {
            var ttt = getChildren(parentCount, onChildren, childrenFilter: childrenFilter);

            List<string> ar = new List<string>();

            foreach (var tttt in ttt)
            {
                var ssss = tttt.Serialize(parentCount, childrenFilter);

                if (!string.IsNullOrEmpty(ssss))
                    ar.Add(ssss);
            }

            return ar.ToArray();
        }

        public enum GettingChildrenFilter
        {
            Superior,
            Inferior
        }

        internal IEnumerable<DIV> getChildren(int parentCount = 0, bool onChildrens = false, DIV not = null, GettingChildrenFilter childrenFilter = GettingChildrenFilter.Inferior)
        {
            int maxWideness = 20;

            lock (this)
            {

                var result = Children.Where(
                    x =>
                        (!onChildrens || (childrenFilter == GettingChildrenFilter.Superior ?
                            x.CollapsedWeight(this) > this.Weight :
                            x.CollapsedWeight(this) <= this.Weight)) &&

                        !x.IsRendered &&
                        (parentCount != 0 || x.Index) &&
                        not != x &&
                        SearchResult.GetDeepDistance(x, VirtualAttributes.MIME_TYPE_DIRECTORY) != 0
                        ).

                    OrderByDescending(

                        x =>
                            SearchResult.GetDeepDistance(x, VirtualAttributes.CONCEITO) == 0 &&
                            SearchResult.GetDeepDistance(x, VirtualAttributes.CONTEUDO) == 0
                        ).

                    ThenByDescending(

                    x =>
                        LogAndReturnWeight(x)).  // Weight // .Children.Average(z => z.Weight)

                    Take(maxWideness);

                return result;
            }
        }

        double LogAndReturnWeight(DIV item)
        {
            var w = 
                SearchResult.GetDeepDistance(item, VirtualAttributes.CONCEITO) == 0 &&
                SearchResult.GetDeepDistance(item, VirtualAttributes.CONTEUDO) == 0 ?
                item.CollapsedWeight(this) : item.AverageChildrenWeight;

            //Log.Write(item.ToString() + "\t" + w.ToString(), 1);

            return w;
        }


        double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }


    }

}
