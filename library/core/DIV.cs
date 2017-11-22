using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class DIV
    {
        public string simpleAddress
        {
            get { return Utils.ToSimpleAddress(Address); }
        }

        internal byte[] Address = new byte[pParameters.addressSize];

        internal long Id;

        internal byte[] Hash = null;

        internal Metapacket Src = null;

        internal Dictionary<long, DIV> Children = new Dictionary<long, DIV>();

        internal Dictionary<long, DIV> ChildrenAddedSinceLastInvalidation = new Dictionary<long, DIV>();

        internal Dictionary<long, DIV> Parents = new Dictionary<long, DIV>();

        internal double Weight = 1;

        internal double NewWeight = 1;

        internal double RelativeWeight = 1;

        internal double AverageChildrenWeight = 1;

        internal bool IsValid = false;

        bool isRendered = false;

        public bool IsRendered
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

        int itemCount = 0;



        SearchResult Result { get; set; }

        internal DIV(SearchResult result, byte[] address = null)
        {
            Result = result;

            if (null == address)
                return;

            this.Address = address;

            this.Id = BitConverter.ToInt64(address, 0);
        }

        internal Dictionary<long, int> Distances = new Dictionary<long, int>();

        internal double UncollapsedWeight(DIV parent)
        {
            if (Src == null)
                return Weight;

            DIV target = null;

            this.Children.TryGetValue(Src.IdTargetAddress, out target);

            DIV link = null;

            this.Children.TryGetValue(Src.IdLinkAddress, out link);

            if (target == parent)
                return link.Weight;

            if (link == parent)
                return target.Weight;

            return Weight;
        }

        internal double CollapsedWeight(SearchResult searchResult, DIV parent)
        {
            if (Src == null)
                return Weight;

            DIV target = null;

            this.Children.TryGetValue(Src.IdTargetAddress, out target);

            DIV link = null;

            this.Children.TryGetValue(Src.IdLinkAddress, out link);

            //todo:9:Dont tryGetm just compare the Ids
            if (target == parent)
                return link == null ? Weight : link.Weight;

            if (link == parent)
                return target == null ? Weight : target.Weight;

            return Weight;
        }

        internal static DIV Find(Dictionary<long, DIV> RootResults, long id)
        {
            DIV result = null;

            RootResults.TryGetValue(id, out result);

            return result;
        }

        //IEnumerable<DIV> FindAll(SearchResult searchResult, long id, Queue<long> parents = null)
        //{
        //    //Log.Write("..." + Utils.ToBase64String(address));

        //    List<DIV> result = new List<DIV>();

        //    if (parents == null)
        //        parents = new Queue<long>();

        //    //
        //    if (parents.Count() < 2 && !parents.Contains(id))
        //    {

        //        if (Addresses.Equals(this.Address, address))
        //            result.Add(this);

        //        else
        //        {
        //            parents.Enqueue(this.Address);

        //            lock (searchResult.LockRootResults)
        //                foreach (var child in Children)
        //                {
        //                    result.AddRange(child.FindAll(searchResult, address, parents));

        //                    //if (result != null)
        //                    //{
        //                    //    foreach (var r in result)
        //                    //        yield return r;
        //                    //}
        //                }

        //            parents.Dequeue();
        //        }

        //    }

        //    return result;
        //}

        //bool FindAny(SearchResult searchResult, byte[] address)
        //{
        //    if (Addresses.Equals(this.Address, address))
        //        return true;

        //    lock (searchResult.LockRootResults)
        //        foreach (var child in Children)
        //        {
        //            if (child.FindAny(searchResult, address))
        //                return true;
        //        }

        //    return false;
        //}

        public string ToString()
        {
            return Utils.ToSimpleAddress(Utils.ToSimpleName(Address));
        }
        

        public void ResetItemCount()
        { 
            itemCount = 0;
        }

        public string Serialize(SearchResult searchResult, RenderMode mode, DIV parent, List<Metapacket> out_result, bool justHierarchy, int parentCount = 0, GettingChildrenFilter childrenFilter = GettingChildrenFilter.Inferior)
        {
            if (this.ToString() == "408")
            { }


            if (IsRendered)
                return string.Empty;

            if (this.IsVirtualAttribute)
                return string.Empty;

            if (this.Weight > parent.Weight)
                return string.Empty;


            var maxDeepness = 11;// Client.MaxDeepness;

            if (childrenFilter == GettingChildrenFilter.Superior)
                maxDeepness = 10;

            Log.Add(Log.LogTypes.Search, Log.LogOperations.Serialize, this);

            if (parentCount > maxDeepness)
                return string.Empty;



            IsRendered = true;
            out_result.Add(this.Src);

            if (SearchResult.GetDeepDistance(this, VirtualAttributes.Id_CONCEITO) == 0 &&
                SearchResult.GetDeepDistance(this, VirtualAttributes.Id_CONTEUDO) == 0)
            {


                if (childrenFilter == GettingChildrenFilter.Inferior)
                    parentCount++;

                if (childrenFilter == GettingChildrenFilter.Inferior)
                    itemCount++;

                string address = "\"address\": \"" + Utils.ToBase64String(this.Address) + "\"";

                string index = "\"index\": \"0\"";

                string date = this.Src == null ? string.Empty : "\"date\": \"" + this.Src.Creation.ToUniversalTime().ToLongDateString() + "\"";

                //var root_type = SearchResult.ClosestMarker(this, VirtualAttributes.ROOT_TYPE);

                var root_type = SearchResult.ClosestMarker(searchResult, this, VirtualAttributes.Id_ROOT_TYPE);

                var root_stype = "post";

                if (root_type != null)
                    if (Addresses.Equals(VirtualAttributes.ROOT_IMAGE, root_type.Address, false))
                    {
                        root_stype = "image";
                    }
                    else if (Addresses.Equals(VirtualAttributes.ROOT_STREAM, root_type.Address, false))
                    {
                        root_stype = "stream";
                    }
                    else if (Addresses.Equals(VirtualAttributes.ROOT_SEQUENCE, root_type.Address, false))
                    {
                        root_stype = "sequence";
                    }

                var root = "\"root\": \"" + root_stype + "\"";

                var m_order = justHierarchy ? null : SearchResult.ClosestMarker(searchResult, this, VirtualAttributes.Id_ORDER);

                var order = string.Empty;

                if (m_order != null)
                {
                    var s_order = SearchResult.FirstContent(searchResult, m_order, VirtualAttributes.Id_MIME_TYPE_TEXT_THUMB, Result.Context, true);

                    order = "\"order\": \"" + s_order + "\"";
                }

                #region AUTHOR

                // var author = string.Empty;

                var author = "\"author\": \"\"";

                /*
                var firstauthor = SearchResult.ClosestMarker(this, VirtualAttributes.AUTHOR);

                if (firstauthor != null)
                {
                    if (!Addresses.Equals(this.Address, firstauthor.Address))
                    {
                        var a = firstauthor.Serialize(mode, parentCount, GettingChildrenFilter.Inferior);

                        //var a = SearchResult.FirstContent(firstauthor, VirtualAttributes.MIME_TYPE_TEXT_THUMB, Result.Context, true);

                        if (!string.IsNullOrWhiteSpace(a))
                            author = "\"author\": " + a + "";
                    }
                }
                */
                #endregion

                #region PIC

                var pic = string.Empty;

                var picAddress = justHierarchy ? null : SearchResult.FirstContent(searchResult, this, VirtualAttributes.Id_MIME_TYPE_IMAGE_THUMB, Result.Context);

                var videoStream = string.Empty;


                var subtitleStreamList = string.Empty;

                var audioStreamList = string.Empty;

                var audioStream = string.Empty;
                var subtitleStream = string.Empty;
                var download = string.Empty;

                pic = "\"pic\": \"" + picAddress + "\"";

                if (this.ToString() == "419" || this.ToString() == "390")
                { }

                if (mode == RenderMode.Nav && parentCount <= 3)
                {
                    var videoResult = Query.Execute("[" + this.Src.Base64TargetAddress + @"]->:LINK->:STREAM
:LINK->?->MIME_TYPE_VIDEO_STREAM");

                    var videoDiv = SearchResult.ClosestMarker(searchResult, this, VirtualAttributes.Id_MIME_TYPE_VIDEO_STREAM);

                    var videoStreamAddres = !videoResult.Valid? string.Empty : Utils.ToBase64String(searchResult.ContextId) + "/" + Utils.ToBase64String(videoResult.Matches[":LINK"][0].Matches[":STREAM"][0].Address) + ":" + Utils.ToBase64String(videoResult.Matches[":LINK"][0].Matches[":STREAM"][0].Hash);

                    videoStream = "\"video\": \"" +  videoStreamAddres + "\"";

                    audioStream = string.Empty;

                    var audioResult = Query.Execute("[" + (videoResult.Valid? Utils.ToBase64String(videoResult.Matches[":LINK"][0].Matches[":STREAM"][0].Address) : this.Src.Base64TargetAddress) + @"]->:LINK->:STREAM
:LINK->?->MIME_TYPE_AUDIO_STREAM");

                    var audioStreamResultAddres = !audioResult.Valid ? string.Empty : Utils.ToBase64String(searchResult.ContextId) + "/" + Utils.ToBase64String(audioResult.Matches[":LINK"][0].Matches[":STREAM"][0].Address) + ":" + Utils.ToBase64String(audioResult.Matches[":LINK"][0].Matches[":STREAM"][0].Hash); 

                    var audioStreamAddres = SearchResult.FirstContent(searchResult, videoDiv == null ? this : videoDiv, VirtualAttributes.Id_MIME_TYPE_AUDIO_STREAM, Result.Context);

                    audioStream = "\"audio\": \"" + audioStreamAddres + "\"";

                    subtitleStream = string.Empty;

                    var subtitleStreamAddres = SearchResult.FirstContent(searchResult, videoDiv == null ? this : videoDiv, VirtualAttributes.Id_MIME_TYPE_TEXT_STREAM, Result.Context);

                    var subtitlesResult = Query.Execute("[" + (videoResult.Valid? Utils.ToBase64String(videoResult.Matches[":LINK"][0].Matches[":STREAM"][0].Address) : this.Src.Base64TargetAddress) + @"]->:LINK->:STREAM
:LINK->?->MIME_TYPE_TEXT_STREAM");

                    var subtitlesStreamResultAddres = !subtitlesResult.Valid ? string.Empty : Utils.ToBase64String(searchResult.ContextId) + "/" + Utils.ToBase64String(subtitlesResult.Matches[":LINK"][0].Matches[":STREAM"][0].Address) + ":" + Utils.ToBase64String(subtitlesResult.Matches[":LINK"][0].Matches[":STREAM"][0].Hash);

                    subtitleStream = "\"subtitle\": \"" + subtitleStreamAddres + "\"";

                    //subtitleStreamList = string.Format("\"subtitles\": [{0}]", SerializeChildren(searchResult, videoDiv, VirtualAttributes.Id_MIME_TYPE_TEXT_STREAM, justHierarchy));

                    //audioStreamList = string.Format("\"audios\": [{0}]", SerializeChildren(searchResult, videoDiv, VirtualAttributes.Id_MIME_TYPE_AUDIO_STREAM, justHierarchy));

                    #endregion



                    download = string.Empty;

                    var downloadAddress = SearchResult.FirstContent(searchResult, this, VirtualAttributes.Id_MIME_TYPE_DOWNLOAD, Result.Context);

                    download = "\"download\": \"" + downloadAddress + "\"";

                }

                var text = justHierarchy ? null : SearchResult.FirstContent(searchResult, this, VirtualAttributes.Id_MIME_TYPE_TEXT, Result.Context, true);

                text = "\"text\": \"" + text + "\"";

                var thumb_text = justHierarchy ? null : SearchResult.FirstContent(searchResult, this, VirtualAttributes.Id_MIME_TYPE_TEXT_THUMB, Result.Context, true);

                thumb_text = "\"thumb_text\": \"" + thumb_text + "\"";

                var weight = "\"weight\": \"" + this.Weight.ToString("n2") + "\"";


                var simpleAddressAttr = "\"simple\": \"" + Utils.ToSimpleAddress(this.Address) + "\"";

                var average = "\"average\": \"" + this.AverageChildrenWeight.ToString("n2") + "\"";

                var collapsed = "\"collapsed\": \"" + CollapsedWeight(searchResult, this).ToString("n2") + "\"";

                itemCount++;


                var inferiorChildren = string.Empty;

                var superiorChildren = string.Empty;

                if (this.ToString() == "476")
                { }

                if (childrenFilter == GettingChildrenFilter.Inferior)
                {
                    var inferiorChildrenList = SerializeChildren(searchResult, mode, this, out_result, justHierarchy, parentCount, true, GettingChildrenFilter.Inferior);

                    if (inferiorChildrenList.Any())
                        inferiorChildren = string.Format("\"children\": [{0}]", string.Join(", ", inferiorChildrenList));
                }

                if (this.Index)
                {
                    var superiorChildrenList = SerializeChildren(searchResult, mode, this, out_result, justHierarchy, parentCount, true, GettingChildrenFilter.Superior);

                    if (superiorChildrenList.Any())
                        superiorChildren = string.Format("\"superiorchildren\": [{0}]", string.Join(", ", superiorChildrenList));
                }

                var results = new string[] { audioStreamList, subtitleStreamList, order, thumb_text, address, index, weight, date, root, text, pic, videoStream, audioStream, subtitleStream, author, download, inferiorChildren, superiorChildren }.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                if (results.Any())
                {
                    results.Add(simpleAddressAttr);

                    results.Insert(0, average);

                    results.Insert(0, collapsed);

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
                    var inferiorChildrenList = SerializeChildren(searchResult, mode, parent, out_result, justHierarchy, parentCount, false, childrenFilter);

                    if (inferiorChildrenList.Any())
                        return string.Join(", ", inferiorChildrenList);
                }
                return string.Empty;
            }

        }

        private string SerializeChildren(SearchResult searchResult, DIV item, long marker, bool justHierarchy)
        {
            var DIVSubtitles = SearchResult.ClosestMarkerList(searchResult, item == null ? this : item, marker);

            var legendaResultAddress = new List<string>();

            foreach (var DIVSubtitle in DIVSubtitles)
            {
                if (DIVSubtitle == null)
                    continue;

                DIV validChildren = DivPropertiesList(DIVSubtitle).FirstOrDefault();



                if (null != validChildren)
                {
                    var xxx = SearchResult.FirstContent(searchResult, validChildren, VirtualAttributes.Id_MIME_TYPE_TEXT_THUMB, Result.Context, true);

                    DIV linkDoValidChildren = null;

                    validChildren.Children.TryGetValue(validChildren.Src.IdLinkAddress, out linkDoValidChildren);

                    if (null != linkDoValidChildren)
                    {
                        var xx = SearchResult.FirstContent(searchResult, linkDoValidChildren, VirtualAttributes.Id_MIME_TYPE_TEXT_THUMB, Result.Context, true);

                        legendaResultAddress.Add(string.Format("{{\"thumb_text\": \"{0}\", \"address\": \"{1}:{2}\"}}", xx, Utils.ToBase64String(DIVSubtitle.Address), Utils.ToBase64String(DIVSubtitle.Src.Hash)));
                    }
                }
            }

            return string.Join(", ", legendaResultAddress);
        }


        //Properties are any children which are not from the main metapacket
        private static IEnumerable<DIV> DivPropertiesList(DIV legenda)
        {
            DIV linkDaLegenda = null;

            legenda.Children.TryGetValue(legenda.Src.IdLinkAddress, out linkDaLegenda);

            DIV targetDaLegenda = null;

            legenda.Children.TryGetValue(legenda.Src.IdTargetAddress, out targetDaLegenda);

            DIV addressDaLegenda = null;

            legenda.Children.TryGetValue(legenda.Src.IdAddress, out addressDaLegenda);


            foreach (var filhoDoLinkDaLegenda in legenda.Children.Keys)
            {
                if (filhoDoLinkDaLegenda == linkDaLegenda.Id || filhoDoLinkDaLegenda == legenda.Id || filhoDoLinkDaLegenda == addressDaLegenda.Id)
                    continue;

                yield return legenda.Children[filhoDoLinkDaLegenda];

                break;
            }
        }

        private string[] SerializeChildren(SearchResult searchResult, RenderMode mode, DIV parent, List<Metapacket> out_result, bool justHierarchy, int parentCount, bool onChildren, GettingChildrenFilter childrenFilter)
        {
            var ttt = getChildren(searchResult, parentCount, onChildren, childrenFilter: childrenFilter);

            List<string> ar = new List<string>();

            foreach (var tttt in ttt)
            {
                var ssss = tttt.Serialize(searchResult, mode, parent, out_result, justHierarchy, parentCount, childrenFilter);

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

        internal IEnumerable<DIV> getChildren(SearchResult searchResult, int parentCount = 0, bool onChildrens = false, DIV not = null, GettingChildrenFilter childrenFilter = GettingChildrenFilter.Inferior)
        {
            int maxWideness = 20;

            lock (searchResult.LockRootResults)
            {

                var result = Children.Where(
                    x =>
                        (!onChildrens || (childrenFilter == GettingChildrenFilter.Superior ?

                            x.Value.CollapsedWeight(searchResult, x.Value) > this.Weight :
                            x.Value.CollapsedWeight(searchResult, x.Value) <= this.Weight

                            )) &&

                        !x.Value.IsRendered &&
                        //(parentCount != 0 || x.Index) &&
                        not != x.Value
                        //&& SearchResult.GetDeepDistance(x, VirtualAttributes.MIME_TYPE_DIRECTORY) != 0
                        ).

                    OrderByDescending(

                        x =>
                            SearchResult.GetDeepDistance(x.Value, VirtualAttributes.Id_CONCEITO) == 0 &&
                            SearchResult.GetDeepDistance(x.Value, VirtualAttributes.Id_CONTEUDO) == 0
                        ).

                    ThenByDescending(

                    x =>
                        LogAndReturnWeight(searchResult, x.Value)).  // Weight // .Children.Average(z => z.Weight)

                    Take(maxWideness).Select(x => x.Value);

                return result;
            }
        }

        double LogAndReturnWeight(SearchResult searchResult, DIV item)
        {
            var w =
                SearchResult.GetDeepDistance(item, VirtualAttributes.Id_CONCEITO) == 0 &&
                SearchResult.GetDeepDistance(item, VirtualAttributes.Id_CONTEUDO) == 0 ?
                item.CollapsedWeight(searchResult, this) : item.AverageChildrenWeight;

            //Log.Write(item.ToString() + "\t" + w.ToString(), 1);

            return w;
        }


        double Sigmoid(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }


    }

}
