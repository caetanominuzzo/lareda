using DiffMatchPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace library
{
    public enum RenderMode
    {
        Main,
        List,
        Nav
    }


    public class SearchResult : IDisposable
    {
        public static RenderMode ParseMode(string mode)
        {
            mode = mode.ToUpper();

            if (mode == "NAV")
                return RenderMode.Nav;
            else if (mode == "LIST")
                return RenderMode.List;
            else
                return RenderMode.Main;
        }

        public byte[] ContextId;

        string Term = string.Empty;

        byte[] bTerm = null;

        internal p2pContext Context;

        public RenderMode Mode = RenderMode.Main;

        List<byte[]> SearchedAddresses = new List<byte[]>();

        public DIV RootResults = null;

        public static String LockRootResults = "lockRootResults";

        List<byte[]> AddressestoSearch = new List<byte[]>();

        internal void AddToSearch(IEnumerable<byte[]> addresses)
        {
            Log.Add(Log.LogTypes.Search, Log.LogOperations.Add, addresses.Select(x => Utils.ToSimpleAddress(x)));

            lock (AddressestoSearch)
            {
                foreach (var b in addresses)
                {
                    if (!Addresses.Equals(Addresses.zero, b, true) && !AddressestoSearch.Any(x => Addresses.Equals(x, b)))
                    {
                        //Log.Write("TO SEARCH: " + Utils.ToSimpleAddress(b));

                        AddressestoSearch.Add(b);
                    }
                }
            }
        }

        byte[] GetToSearch()
        {


            lock (AddressestoSearch)
            {
                byte[] result = null;

                if (AddressestoSearch.Any())
                {
                    Log.Add(Log.LogTypes.Search, Log.LogOperations.Get, AddressestoSearch.Select(x => Utils.ToSimpleAddress(x)).Aggregate((x, y) => string.Concat(x, "-", y)));

                    result = AddressestoSearch[0];

                    AddressestoSearch.RemoveAt(0);

                    if (Utils.ToSimpleAddress(result) == "001")
                    { }
                }

                return result;
            }
        }

        public SearchResult(byte[] contextId, string term, RenderMode mode, p2pContext context, SearchResult parent = null)
        {
            Context = context;

            Client.OnSearchReturn += Client_OnSearchReturn;

            Client.OnFileDownload += Client_OnFileDownload;


            ContextId = contextId;

            RootResults = new DIV(this);

            RootResults.Weight = double.MaxValue;

            if (parent != null)
            {
                RootResults = parent.RootResults;
            }

            AddSearch(term, mode);
        }

        public void AddSearch(string term, RenderMode mode)
        {
            Mode = mode;

            var tmpbTerm = Utils.AddressFromBase64String(term);

            //if (term.IndexOf(Term) != 0 || bTerm != null || term == Term)
            ResetResults(!term.Contains(Term) && bTerm == null);

            Term = term;

            if (tmpbTerm != null)
            {
                lock (LockRootResults)
                    bTerm = MetaPackets.LocalizeAddress(tmpbTerm);

                Search(bTerm, MetaPacketType.Link);
            }
            else
            {
                var hash = Utils.ToAddressSizeArray(Term);

                Search(hash, MetaPacketType.Hash);

                var terms = term.Split(' ');

                if (terms.Count() > 1)
                    foreach (var t in terms)
                    {
                        hash = Utils.ToAddressSizeArray(t);

                        Search(hash, MetaPacketType.Hash);
                    }
            }
        }

        public void ResetResults(bool newRoot = false)
        {
            lock (SearchedAddresses)
                SearchedAddresses.Clear();

            if (true) //newRoot)
            {
                lock (LockRootResults)
                    RootResults = new DIV(this);

                lock (AddressestoSearch)
                    AddressestoSearch.Clear();

                Utils.framePrint = string.Empty;
            }
            else
            {
                lock (LockRootResults)
                {
                    RootResults.IsRendered = false;

                    foreach (var t in RootResults.Children)
                    {
                        t.IsRendered = false;

                        t.IsReseted = true;
                    }
                }
            }


        }

        bool Search(byte[] address, MetaPacketType type)
        {

            if(Utils.ToSimpleAddress(address) == "521")
            { }


            var searched = true;

            lock (SearchedAddresses)
            {
                searched = SearchedAddresses.Any(x => Addresses.Equals(x, address));

                if (!searched)
                    SearchedAddresses.Add(address);
            }



            if (!searched)
            {
                //Log.Write("SEARCH: " + Utils.ToSimpleAddress(address));

                Client.Search(address, type);
            }

            return !searched;
        }

        public bool Searched(byte[] address)
        {
            lock (SearchedAddresses)
            {
                return SearchedAddresses.Any(x => Addresses.Equals(x, address, true));
            }
        }

        public void PrepareToRender(DIV item, int parentCount = 0)
        {
            var maxDeepness = Client.MaxDeepness;

            var maxWideness = 20;

            if (item.IsValid)
                return;

            item.IsValid = true;

            if (parentCount > maxDeepness)
                return;

            if (parentCount > 0 && item.AverageChildrenWeight < item.Weight / 100)
                return;

            if (!Searched(item.Address))
                AddToSearch(new byte[][] { item.Address });

            DIV[] children = null;

            lock (LockRootResults)
                children = item.Children.
                Where(
                    x => !x.IsValid // && (x.Weight == 1 || x.AverageChildrenWeight / x.Weight > .5)
                ).
                OrderByDescending(

                x =>
                      SearchResult.GetDeepDistance(x, VirtualAttributes.CONCEITO) == 0 &&
                      SearchResult.GetDeepDistance(x, VirtualAttributes.CONTEUDO) == 0

                ).ThenByDescending(

                x => x.Weight

                ).Take(maxWideness).ToArray();

            parentCount++;

            foreach (var c in children)
                PrepareToRender(c, parentCount);
        }



        List<string> frames = new List<string>();

        bool AddSearchResults(byte[] search, MetaPacketType type, IEnumerable<Metapacket> metapackets)
        {
            Utils.PrintSearchResult(search, type, metapackets);

            if (metapackets.Count() > MetaPackets.MostUsedRatio.Average * 2000)
                return false;

            if (metapackets.Count() == 11221)
            {

            }

            byte[] toSearch = null;

            var anyInvalidation = false;

            if (Monitor.IsEntered(RootResults))
                return false;

            lock (LockRootResults)
            {
                foreach (var m in metapackets)
                {
                    if (RootCreate(m, type))
                        anyInvalidation = true;
                }

                if (anyInvalidation)
                {
                    foreach (var r in RootResults.Children)
                    {
                        if (!r.Children.Any())
                            continue;

                        ConceptInvalidate(r);
                    }

                    return true;
                }

            }

            return false;
        }



        internal void SetDeepDistance(DIV item, int source, byte[] distanceMarker)
        {
            //Log.Write(item.ToString() + "  " + Utils.ToSimpleAddress(distanceMarker), 10+(source * 2));

            var current = 0;

            var hasKey = item.Distances.TryGetValue(distanceMarker, out current);

            if (source > 0 && source >= current && hasKey)
                return;

            if (hasKey)
                item.Distances[distanceMarker] = source;
            else
                item.Distances.Add(distanceMarker, source);

            source++;

            lock (SearchResult.LockRootResults)
                foreach (var p in item.Parents)
                    SetDeepDistance(p, source, distanceMarker);
        }

        internal static int GetDeepDistance(DIV item, byte[] distanceMarker)
        {
            var current = int.MaxValue;

            var hasKey = item.Distances.TryGetValue(distanceMarker, out current);

            if (hasKey)
                return current;

            return int.MaxValue;
        }


        void ConceptInvalidate(DIV item, Stack<DIV> parents = null)
        {
            if (parents == null)
                parents = new Stack<DIV>();

            else if (parents.Any(x => Addresses.Equals(x.Address, item.Address)))
                return;



            double newWeight = 2;

            //newWeight = item.Children.Sum(x => x.Children.Any() ? x.Weight / x.Children.Count() : (double)1);// / item.Children.Count(); //item.Children.Sum(x => x.Weight) /

            lock (LockRootResults)
                newWeight = item.Children.Sum(x => x.RelativeWeight);

            //if(item.Children.Count() > 1)// && invalidator.Children.Count() > 1)


            if (newWeight == 0)
                newWeight = 1;



            var end = !item.IsValid &&
                Math.Abs(item.Weight - newWeight) < (item.Weight / 5);

            if (end || parents.Count() > Client.MaxDeepness)
                return;




            //Log.Write(Utils.ToSimpleAddress(item.Address) + "\t" +
            //    item.Weight.ToString("n2") + "\t" +

            //    item.AverageChildrenWeight.ToString("n2") + "\t" +
            //    //item.CollapsedWeight.ToString("n2") + "\t" +
            //    SearchResult.FirstContent(item, null, null, VirtualAttributes.MIME_TYPE_TEXT_THUMB, true)
            //    , parents.Count() + 1);


            //Log.Write(Utils.ToSimpleAddress(item.Address) + "\t" + item.Weight.ToString("n2") + "\t" + newWeight.ToString("n2") + "\t" + item.IsValid + "\t" + childavg.ToString("n2"), parents.Count() + 1);

            item.Weight = newWeight;

            item.RelativeWeight = newWeight / item.Children.Count();

            item.AverageChildrenWeight = item.Children.Average(x => x.Weight);

            item.IsValid = false;

            item.IsRendered = false;

            parents.Push(item);


            lock (SearchResult.LockRootResults)
                foreach (var p in item.Parents)
                {
                    // if(p.Weight > item.Weight)
                    ConceptInvalidate(p, parents);
                }

            parents.Pop();

            //var concept = ClosestMarker(item, DIV.DISTANCE_MARKERS.Concept);

            //if (concept != null)
            //{
            //    concept.IsValid = false;

            //    concept.IsRendered = false;
            //}




        }

        internal bool ChildrenAdd(DIV t, DIV item)
        {
            lock (LockRootResults)
                if (!t.Children.Any(x => Addresses.Equals(x.Address, item.Address)))
                {
                    t.Children.Add(item);

                    item.Parents.Add(t);

                    if (item.Src != null && t.Src == null)
                        t.Src = item.Src;

                    //foreach (var m in t.Distances.Keys)
                    //    SetDeepDistance(item, t.Distances[m] + 1, m);

                    return true;
                }
                else
                {
                    if (t.IsReseted)
                    {
                        t.IsReseted = false;

                        t.IsValid = false;

                        return true;
                    }

                }


            return false;
        }

        internal bool RootCreate(Metapacket metapacket, MetaPacketType type)
        {
            var addressToSearch = new List<byte[]>();

            var anyInvalidation = false;

            var link = RootAddItem(metapacket.LinkAddress, metapacket.Hash, metapacket, out anyInvalidation);

            if (type == MetaPacketType.Hash)
                return true;


            var address = RootAddItem(metapacket.Address, null, metapacket, out anyInvalidation);


            if(Utils.ToSimpleAddress(metapacket.Address) == "521" ||
                Utils.ToSimpleAddress(metapacket.LinkAddress) == "521" ||
                Utils.ToSimpleAddress(metapacket.TargetAddress) == "521")
            { }

            if (ChildrenAdd(address, link))
                anyInvalidation = true;

            if (ChildrenAdd(link, address))
                anyInvalidation = true;


            var target = RootAddItem(metapacket.TargetAddress, null, null, out anyInvalidation);



            if (ChildrenAdd(target, address))
                anyInvalidation = true;

            if (ChildrenAdd(address, target))
                anyInvalidation = true;

            if (metapacket.Marker != null)
                SetDeepDistance(target, 0, metapacket.Marker);

            // if (metapacket.Marker != null)
            //      SetDeepDistance(link, 0, metapacket.Marker);



            //if (Addresses.Equals(link.Address, VirtualAttributes.AUTHOR))
            //    SetDeepDistance(link, 0, (int)DIV.DISTANCE_MARKERS.Author);

            //else if (Addresses.Equals(link.Address, VirtualAttributes.CONCEITO))
            //    SetDeepDistance(link, 0, (int)DIV.DISTANCE_MARKERS.Concept);

            //else if (metapacket.Hash != null)
            //    SetDeepDistance(link, 0, (int)DIV.DISTANCE_MARKERS.Content);


            return anyInvalidation;
        }


        internal DIV RootAddItem(byte[] address, byte[] hash, Metapacket metapacket, out bool anyInvalidation)
        {
            anyInvalidation = false;

            if (Utils.ToSimpleAddress(address) == "001")
            { }

            var result = Find(address);

            if (result != null)
            {
                result.Hash = hash ?? result.Hash;

                result.Src = metapacket ?? result.Src;

                anyInvalidation = false;

                return result;
            }

            result = new DIV(this);

            result.Address = address;

            result.Hash = hash;

            result.Src = metapacket;

            result.Index = (metapacket != null && metapacket.Type == MetaPacketType.Hash) || (bTerm != null && Addresses.Equals(bTerm, address));

            //pra nao passar de um conceito pro outro aleatoriamente enquanto nao estabiliza os resultados
            if (VirtualAttributes.IsVirtualAttribute(result.Address))
            //if (int.Parse(Utils.ToSimpleAddress(result.Address)) < VirtualAttributes.Count)
            {
                result.IsVirtualAttribute = true;
            }

            if (ChildrenAdd(RootResults, result))
                anyInvalidation = true;

            return result;
        }

        DIV Find(byte[] address)
        {
            lock (LockRootResults)
                foreach (var c in RootResults.Children)
                    if (Addresses.Equals(c.Address, address))
                        return c;

            return null;
        }

        void Client_OnFileDownload(byte[] address, string filename, string speficFilena, int[] arrives, int[] cursors)
        {
        }

        void Client_OnSearchReturn(byte[] search, MetaPacketType type, IEnumerable<Metapacket> metapackets)
        {
            Log.Add(Log.LogTypes.Search, Log.LogOperations.Incoming, new { search = Utils.ToSimpleAddress(search), metapackets });

            if (Utils.ToSimpleAddress(search) == "380")
            { }

            lock (LockRootResults)

                if (Searched(search))
                {
                    var anyInvalidation = AddSearchResults(search, type, metapackets);

                    PrepareToRender(RootResults);

                    byte[] toSearch = GetToSearch();

                    while (toSearch != null)
                    {
                        Search(toSearch, MetaPacketType.Link);

                        toSearch = GetToSearch();
                    }
                }
        }

        public string GetResultsResults(p2pContext context)
        {
            if (Monitor.IsEntered(RootResults))
                return "[]";

            Context = context;

            lock (LockRootResults)
            {

                var root = RootResults;

                //if (bTerm != null)
                //    root = RootResults.Children.FirstOrDefault(x => Addresses.Equals(x.Address, bTerm));

                //if (root == null)
                //    root = RootResults;

                //Log.Write(string.Join(Environment.NewLine,
                //    RootResults.Children.OrderByDescending(x => x.Weight).Take(RootResults.Children.Count() / 10).Select(x =>

                //        Utils.ToSimpleAddress(x.Address) + "\t" +
                //        x.Weight.ToString("n2") + "\t" +
                //         x.AverageChildrenWeight.ToString("n2") + "\t" +
                //         (
                //            x.Distances[(int)DIV.DISTANCE_MARKERS.Concept] == 0 &&
                //            x.Distances[(int)DIV.DISTANCE_MARKERS.Content] == 0 ? FirstContent(x) :
                //                int.Parse(Utils.ToSimpleAddress(x.Address)) < VirtualAttributes.Count ? VirtualAttributes.PropertyIndex(int.Parse(Utils.ToSimpleAddress(x.Address))) : string.Empty

                //         )
                //        )

                //         ));

                if (root.Children.Count() > 4)
                { }

                root.ResetItemCount();

                var ar1 = string.Empty;

                if (Mode == RenderMode.Nav && Find(bTerm) != null)
                    ar1 = Find(bTerm).Serialize(Mode, root);
                else
                    ar1 = root.Serialize(Mode, root);

                //if(ar1.Length > 0)
                //    ar1 = "{\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"wA08ZlhXpGhS0qO3Xx7XgdyPgZjBWQSdiKsez4_hmY4=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"727\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"-vulMifhA5fMzdx5AT3G_coJ4ime2yZvZkcrKXIa1cM=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"734\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"uhZqrmTdHoo9mGMiwfBGB2egEP_eNjBpQGjT1rcKGQY=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"758\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"dSMRDLDF1CZb9GPYC1UiP7BIHyBWP8wsVd3siMeI1XA=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"768\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"9Hh_qJk2V4kmmzZxjen0yxnYEAjSC5oRJ4j14bsKQB4=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"783\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"UUkLuHRf81IEejvelG0Izv0lJFy2ItL9ALv7-ALCPUo=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"793\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"tdNHWGhh2Dr2Wp-7S0E6xZsQ1nLn1efTuCN_vYQCc9U=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"802\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"bLiay5t4uItTcPuEmM9Xw7z06dyrzUYIn3VugRBANys=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"809\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"IHPPNTGWuo0SDe-1pP666r-I0SxYaWFWerl7OLyCUZc=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"816\"}, {\"root\": \"stream\", \"collapsed\": \"4.00\", \"average\": \"2.50\", \"thumb_text\": \"ccc10\", \"address\": \"BX-NkbLDXccmVic6x5y7xkw1KHzaslaL5Q1pi18H2K0=\", \"index\": \"0\", \"weight\": \"4.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"826\"}, {\"root\": \"stream\", \"collapsed\": \"3.00\", \"average\": \"2.67\", \"thumb_text\": \"ddd10\", \"address\": \"iIG1801PafGy_IJ5er7tPQ-_JtoGh4h7ScpqlQDtzHM=\", \"index\": \"0\", \"weight\": \"3.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"741\"}, {\"root\": \"stream\", \"collapsed\": \"3.00\", \"average\": \"2.67\", \"thumb_text\": \"eee10\", \"address\": \"G6tE_mp-m7BRWhtl8dfql0E93_K-te4uQyh7RE2vr7A=\", \"index\": \"0\", \"weight\": \"3.00\", \"date\": \"Sunday, April 9, 2017\", \"text\": \"\", \"pic\": \"dTDBn0k_zLDM8hkxwaOvv3RX0tM0WPeQ-h-EHxC5A9M=\", \"simple\": \"748\"}";

                return "[" + ar1 + "]";

            }


        }


        internal static bool logging = false;

        internal static string FirstContent(DIV item, byte[] marker, p2pContext context, bool text = false)
        {
            var t = marker == null ? item : ClosestMarker(item, marker);

            //Log.Write("packet get " + Utils.ToBase64String(t.Address));

            if (t == null)
                return string.Empty;

            return text ?

                Content(t.Src.LinkAddress, context) :

                 Utils.ToBase64String(t.Src.LinkAddress);
        }

        internal static IEnumerable<string> FirstContentYield(DIV item, byte[] marker, p2pContext context, bool text = false)
        {
            List<string> result = new List<string>();

            var t = ClosestMarkerList(item, marker);

            foreach (var tt in t)
            {
                if (tt != null)
                    result.Add(text ?

                Content(tt.Src.LinkAddress, context) :

                 Utils.ToBase64String(tt.Src.LinkAddress));
            }

            //Log.Write("packet get " + Utils.ToBase64String(t.Address));

            return result;
        }



        internal static string Content(byte[] address, p2pContext context)
        {
            var packet = Packets.Get(address);



            if (packet != null)
            {
                var s = Encoding.Unicode.GetString(packet.Skip(pParameters.packetHeaderSize).ToArray()).Replace("\\", "\\\\").Replace(Environment.NewLine, "\\n").Replace("\"", "\\\"").Trim();

                return s == null ? string.Empty : s;
            }
            else
            {
                p2pFile.Queue.Add(Utils.ToBase64String(address), context, Utils.ToBase64String(address));
            }

            return string.Empty;
        }

        internal static string Content(DIV item, p2pContext context)
        {
            if (item == null)
                return string.Empty;

            return Content(item.Address, context);


        }

        internal static string FirstContent(DIV item, p2pContext context, List<DIV> searched = null, DIV root = null, byte[] MIME_TYPE = null, bool text = true, Stack<DIV> parents = null)
        {
            //    if (item.simpleAddress == "552" && text && parents == null)
            //    logging = true;


            if (parents == null)
                parents = new Stack<DIV>();
            else
            {
                if (parents.Count() > (true ? 2 : 5))
                    return string.Empty;
            }

            //if (logging)
            //    Log.Write(Utils.ToSimpleAddress(item.Address), parents.Count() + 1);

            if (item == null)
                return string.Empty;

            if (searched == null)
                searched = new List<DIV>();

            if (root == null)
                root = item;

            //  if (searched.Any(x => Addresses.Equals(x.Address, item.Address)))
            //      return string.Empty;

            searched.Add(item);

            DIV content = null;

            lock (SearchResult.LockRootResults)
                content = item.Children.FirstOrDefault(x => x.Hash != null);

            if (content != null)
            {
                var any = false;

                lock (SearchResult.LockRootResults)
                    any = MIME_TYPE == null || (text ? item : item).Children.Any(x => x.Children.Any(y => Addresses.Equals(y.Address, MIME_TYPE)));

                if (any)
                {
                    //Log.Write("packet get " + Utils.ToBase64String(content.Address));

                    var packet = Packets.Get(content.Address);

                    if (packet != null)
                    {
                        //if (text)
                        //    Log.Write("OK: " + Encoding.Unicode.GetString(packet.Skip(Parameters.packetHeaderSize).ToArray()));

                        logging = false;

                        return (text) ?

                            Content(content, context) :

                            Utils.ToBase64String(content.Address);
                    }
                }
            }
            else
            {
                IEnumerable<DIV> list = null;

                lock (SearchResult.LockRootResults)
                    list = item.Children;

                if (list.Any())
                {
                    parents.Push(item);

                    lock (SearchResult.LockRootResults)
                        list = item.Children.
                            Where(x => !searched.Any(y => Addresses.Equals(x.Address, y.Address))).
                            OrderBy(x => x.Hash == null).
                            //ThenBy(x =>  x.Distances[(int)DIV.DISTANCE_MARKERS.Content]);
                            ThenBy(x => SearchResult.GetDeepDistance(x, VirtualAttributes.CONTEUDO));

                    foreach (var l in list)
                    {
                        var s = FirstContent(l, context, searched, null, MIME_TYPE, text, parents);

                        if (!string.IsNullOrEmpty(s))
                        {

                            return s;
                        }
                    }

                    parents.Pop();
                }
            }

            return string.Empty;
        }

        internal static DIV ClosestMarker(DIV item, byte[] marker, List<DIV> searched = null, DIV root = null, byte[] predicate = null, Stack<DIV> parents = null)
        {
            if (parents == null)
                parents = new Stack<DIV>();
            else
            {
                if (parents.Count() > 8)
                    return null;
            }

            if (searched == null)
                searched = new List<DIV>();

            if (root == null)
                root = item;

            if (searched.Any(x => Addresses.Equals(x.Address, item.Address)))
                return null;

            searched.Add(item);



            if (item != null && SearchResult.GetDeepDistance(item, marker) == 0)
            //if (item.Distances[(int)marker] == 0)
            {
                if (predicate == null || item.Children.Any(
                    x => Addresses.Equals(x.Address, predicate)))
                {
                    var target = DIV.Find(item.Children, item.Src.TargetAddress);

                    var link = DIV.Find(item.Children, item.Src.LinkAddress);

                    if (target == root)
                        item = link;

                    else if (link == root)
                        item = target;

                    return item;
                }
            }
            else
            {
                

                List<DIV> list = null;

                lock (SearchResult.LockRootResults)
                    list = item.Children.OrderBy(x => 1).ToList();

                if (list.Any())
                {
                    parents.Push(item);

                    lock (SearchResult.LockRootResults)
                        list = item.Children.
                            OrderBy(x => SearchResult.GetDeepDistance(x, marker)).ToList();

                    foreach (var l in list)
                    {
                        if (l.ToString() == "377")
                        {

                        }

                        var s = ClosestMarker(l, marker, searched, root, predicate, parents);

                        if (s != null)
                        {
                            parents.Pop();

                            return s;
                        }
                    }

                    parents.Pop();
                }
            }

            return null;
        }

        internal static IEnumerable<DIV> ClosestMarkerList(DIV item, byte[] marker, List<DIV> searched = null, DIV root = null, byte[] predicate = null, Stack<DIV> parents = null)
        {
            if (parents == null)
                parents = new Stack<DIV>();

            if (parents.Count() < 9)
            {

                if (searched == null)
                    searched = new List<DIV>();

                if (root == null)
                    root = item;

                if (!searched.Any(x => Addresses.Equals(x.Address, item.Address)))
                {

                    searched.Add(item);



                    if (SearchResult.GetDeepDistance(item, marker) == 0)
                    //if (item.Distances[(int)marker] == 0)
                    {
                        if (predicate == null || item.Children.Any(
                            x => Addresses.Equals(x.Address, predicate)))
                        {
                            var target = DIV.Find(item.Children, item.Src.TargetAddress);

                            var link = DIV.Find(item.Children, item.Src.LinkAddress);

                            if (target == root)
                                item = link;

                            else if (link == root)
                                item = target;

                            yield return item;
                        }
                    }
                    else
                    {
                        IOrderedEnumerable<DIV> list = null;

                        lock (SearchResult.LockRootResults)
                            list = item.Children.OrderBy(x => 1);

                        if (list.Any())
                        {
                            parents.Push(item);

                            lock (SearchResult.LockRootResults)
                                list = item.Children.
                                    OrderBy(x => SearchResult.GetDeepDistance(x, marker));

                            foreach (var l in list)
                            {
                                var s = ClosestMarkerList(l, marker, searched, root, predicate, parents);

                                if (s != null)
                                {
                                    if (parents.Any())
                                        parents.Pop();

                                    foreach (var ss in s)
                                        if (ss != null)
                                            yield return ss;
                                }
                            }

                            if (parents.Any())
                                parents.Pop();
                        }
                    }

                    //yield return null;
                }
            }
        }

        public void Dispose()
        {
            Client.OnFileDownload -= Client_OnFileDownload;

            Client.OnSearchReturn -= Client_OnSearchReturn;
        }
    }

}

