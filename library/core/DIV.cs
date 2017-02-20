using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class DIV
    {
        internal byte[] filter;

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


        public string Serialize(byte[] filter, int parentCount = 0, GettingChildrenFilter childrenFilter = GettingChildrenFilter.Inferior)
        {
            this.filter = filter;

            if (IsRendered)
                return string.Empty;

            var maxDeepness = filter == null? 1 : 3;// Client.MaxDeepness;

            if (childrenFilter == GettingChildrenFilter.Superior)
                maxDeepness = 3;



            if (parentCount > maxDeepness)
                return string.Empty;

            //Log.Write(Utils.ToSimpleAddress(this.Address) + "\t" +
            //    this.Weight.ToString("n2") + "\t" +

            //    this.AverageChildrenWeight.ToString("n2") + "\t" +
            //    //this.CollapsedWeight.ToString("n2") + "\t" +
            //    SearchResult.FirstContent(this, null, null, VirtualAttributes.MIME_TYPE_TEXT_THUMB, true)
            //    , parentCount + 1);


            IsRendered = true;

            if(this.Children.Any(x => Utils.ToSimpleAddress(x.Address)=="379" ))
            { }

            if(filter != null && this.Children.Count() > 0)
            {

            }


            if (Utils.ToSimpleAddress(this.Address) == "687")
            { }

            Log.Write("printing: " + Utils.ToSimpleAddress(this.Address), 10 + parentCount);

            if ((filter == null && SearchResult.GetDeepDistance(this, VirtualAttributes.CONCEITO) == 0 && 
                SearchResult.GetDeepDistance(this, VirtualAttributes.CONTEUDO) == 0) ||
                    (filter != null && SearchResult.GetDeepDistance(this, filter) == 0))
               // && (filter == null || SearchResult.GetDeepDistance(this, filter) == 0))
            {
                if (Utils.ToSimpleAddress(this.Address) == "687")
                { }

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
                        var a = firstauthor.Serialize(filter, parentCount);

                        if (!string.IsNullOrWhiteSpace(a))
                            author = "\"author\": " + a + "";
                    }
                }

                #endregion

                #region PIC

                var pic = string.Empty;

                var picAddress = SearchResult.FirstContent(this, VirtualAttributes.MIME_TYPE_IMAGE_THUMB);


                var videoStream = string.Empty;

                var videoDiv = SearchResult.ClosestMarker(this, VirtualAttributes.MIME_TYPE_VIDEO_STREAM);

                var videoStreamAddres = videoDiv == null ? string.Empty : Utils.ToBase64String(videoDiv.Address);

                videoStream = "\"video\": \"" + videoStreamAddres + "\"";


                if (picAddress.Length == 0 && videoStreamAddres.Length == 0 && firstauthor != null)
                    picAddress = SearchResult.FirstContent(firstauthor, VirtualAttributes.MIME_TYPE_IMAGE_THUMB);

                pic = "\"pic\": \"" + picAddress + "\"";


                if (picAddress == "9lJR8befQ535xFiwC5gDsL2WC45C8EN5bxKZT5ACHYA=")
                {

                }

                var audioStream = string.Empty;

                var audioStreamAddres = SearchResult.FirstContent(videoDiv == null ? this : videoDiv, VirtualAttributes.MIME_TYPE_AUDIO_STREAM);

                audioStream = "\"audio\": \"" + audioStreamAddres + "\"";

                var subtitleStream = string.Empty;

                var subtitleStreamAddres = SearchResult.FirstContent(videoDiv == null ? this : videoDiv, VirtualAttributes.MIME_TYPE_TEXT_STREAM);

                subtitleStream = "\"subtitle\": \"" + subtitleStreamAddres + "\"";

                #endregion


                var download = string.Empty;

                var downloadAddress = SearchResult.FirstContent(this, VirtualAttributes.MIME_TYPE_DOWNLOAD);

                download = "\"download\": \"" + downloadAddress + "\"";



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
                    var inferiorChildrenList = SerializeChildren(filter, parentCount, true, GettingChildrenFilter.Inferior);

                    if (inferiorChildrenList.Any())
                        inferiorChildren = string.Format("\"children\": [{0}]", string.Join(", ", inferiorChildrenList));
                }

                if (this.Index)
                {
                    var superiorChildrenList = SerializeChildren(filter, parentCount, true, GettingChildrenFilter.Superior);

                    if (superiorChildrenList.Any())
                        superiorChildren = string.Format("\"superiorchildren\": [{0}]", string.Join(", ", superiorChildrenList));
                }

                var results = new string[] { thumb_text, address, index, weight, date, text, pic, videoStream, audioStream, subtitleStream, author, download, inferiorChildren, superiorChildren }.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

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
                    var inferiorChildrenList = SerializeChildren(filter, parentCount, false, childrenFilter);

                    if (inferiorChildrenList.Any())
                        return string.Join(", ", inferiorChildrenList);
                }
                return string.Empty;
            }

        }

        private string[] SerializeChildren(byte[] filter, int parentCount, bool onChildren, GettingChildrenFilter childrenFilter)
        {
            var ttt = getChildren(parentCount, onChildren, childrenFilter: childrenFilter);

            List<string> ar = new List<string>();

            foreach (var tttt in ttt)
            {
                var ssss = tttt.Serialize(filter, parentCount, childrenFilter);

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
                        (filter != null || !onChildrens || (childrenFilter == GettingChildrenFilter.Superior ?
                            x.CollapsedWeight(this) > this.Weight :
                            x.CollapsedWeight(this) <= this.Weight)) &&

                        !x.IsRendered &&
                        (parentCount != 0 || x.Index) &&
                        not != x &&
                        SearchResult.GetDeepDistance(x, VirtualAttributes.MIME_TYPE_DIRECTORY) != 0
                        ).

                    OrderByDescending(

                        x =>
                            (filter == null && SearchResult.GetDeepDistance(x, VirtualAttributes.CONCEITO) == 0 &&
                            SearchResult.GetDeepDistance(x, VirtualAttributes.CONTEUDO) == 0) || (filter != null && SearchResult.GetDeepDistance(x, filter) == 0)
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
            var w = (filter == null &&
                SearchResult.GetDeepDistance(item, VirtualAttributes.CONCEITO) == 0 &&
                SearchResult.GetDeepDistance(item, VirtualAttributes.CONTEUDO) == 0) ||
                (filter != null && SearchResult.GetDeepDistance(item, filter) == 0)
                ? item.CollapsedWeight(this) : item.AverageChildrenWeight;

            //Log.Write(item.ToString() + "\t" + w.ToString(), 1);

            return w;
        }


        double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }


    }

}
