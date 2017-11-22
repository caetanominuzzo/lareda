using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class Query
    {
        public class Node
        {
            public byte[] Address = null;

            public MetaPacketType Type = MetaPacketType.Link;

            public string Name = null;

            public static Node Parse(string node)
            {
                var result = new Node();

                if (node == "?")
                    return result;

                var parts = node.Split(' ');

                if (parts[0][0] == '[')
                {
                    result.Address = Utils.AddressFromBase64String(parts[0].Replace("[", "").Replace("]", ""));

                    if (parts.Length > 1)
                        result.Name = parts[1];
                }
                else if (parts[0][0] == '\'')
                {
                    result.Type = MetaPacketType.Hash;

                    var clean_parts = parts[0].Replace("'", "");

                    var address = Utils.AddressFromBase64String(clean_parts);

                    if (null == address)
                        address = Utils.ToAddressSizeArray(clean_parts);

                    result.Address = address;

                    if (parts.Length > 1)
                        result.Name = parts[1];
                }
                else if (parts[0][0] == ':')
                {
                    result.Name = parts[0];
                }
                else
                {
                    result.Address = VirtualAttributes.IsVirtualAttribute(parts[0]);
                }

                return result;
            }
        }

        class Triple
        {
            public Node Target = null;

            public Node Address = null;

            public Node Link = null;

            public bool RequireHash = false;

            public static List<Triple> Parse(string sql)
            {
                var lines = sql.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                var result = new List<Triple>();

                foreach (var line in lines)
                {
                    result.Add(ParseLine(line));
                }

                return result;
            }

            public static Triple ParseLine(string line)
            {
                var nodes = line.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);

                var result = new Triple();

                result.Target = Node.Parse(nodes[0]);

                result.Address = Node.Parse(nodes[1]);

                result.Link = Node.Parse(nodes[2]);

                if (nodes[2].Last() == '#')
                    result.RequireHash = true;

                return result;
            }
        }

        static bool Execute(List<Metapacket> search_metapackets, List<Triple> triples, List<byte[]> result, NodeResult noderesult, int offset = 0)
        {
            if (offset == triples.Count)
                return true;

            var triple = triples[offset];


            if (noderesult.Simple == "353")
            {

            }

            if (triple.Target.Type == MetaPacketType.Hash)
            {
                var rs = MetaPackets.LocalSearch(triple.Target.Address, MetaPacketType.Hash);

                var found = rs.Any();

                foreach (var r in rs)
                {
                    result.Add(r.LinkAddress);

                    var child = new NodeResult(noderesult, triple.Target.Name, r.LinkAddress);

                    noderesult.Children.Add(child);

                    triple.Target.Type = MetaPacketType.Link;

                    found = found && Execute(search_metapackets, triples, result, child, offset);
                }

                return found;
            }

            var new_result = new List<byte[]>();

            if (null != triple.Link.Address)
            {
                if (triple.Target.Name != noderesult.Name)
                {
                    offset++;

                    return Execute(search_metapackets, triples, result, noderesult, offset);
                }

                var found = false;

                foreach (var x in MetaPackets.Links.Items)
                {
                    foreach (var y in x.Value)
                    {
                        if (Addresses.Equals(y.TargetAddress, noderesult.Address, false) &&
                            Addresses.Equals(y.LinkAddress, triple.Link.Address, false))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        break;
                }

                if (!found)
                {
                    noderesult.Valid = false;

                    return false;
                }
                else
                {
                    offset++;

                    return Execute(search_metapackets, triples, new_result, noderesult, offset);
                }
            }
            else
            {

                if (null == triple.Target.Address && triple.Target.Name != noderesult.Name)
                {
                    offset++;

                    return Execute(search_metapackets, triples, result, noderesult, offset);
                }
                
                if(null != triple.Target.Address && noderesult.Name == ":ROOT")
                {
                    noderesult.Address = triple.Target.Address;
                }

                offset++;

                var found = false;

                var inner_found = false;

                //foreach (var x in MetaPackets.Links.Items)
                {
                    //foreach (var y in x.Value)
                    foreach (var y in search_metapackets)

                    {
                        if (Addresses.Equals(y.TargetAddress, noderesult.Address, false) &&
                            (!triple.RequireHash || null != y.Hash))
                        {
                            inner_found = true;

                            NodeResult child1 = null;

                            NodeResult child2 = null;

                            if (null != triple.Address.Name && (triple.Target.Name == noderesult.Name || null != triple.Target.Address))
                            {
                                new_result.Add(y.Address);

                                child2 = new NodeResult(noderesult, triple.Address.Name, y.Address);

                                noderesult.Children.Add(child2);

                                noderesult.Valid = true;

                                inner_found = Execute(search_metapackets, triples, result, child2, offset);
                            }

                            if (null != triple.Link.Name && (triple.Target.Name == noderesult.Name || null != triple.Target.Address))
                            {
                                new_result.Add(y.LinkAddress);

                                child1 = new NodeResult((null == child2? noderesult : child2), triple.Link.Name, y.LinkAddress, y.Hash);

                                (null == child2 ? noderesult : child2).Children.Add(child1);

                                (null == child2 ? noderesult : child2).Valid = true;

                                inner_found = inner_found && Execute(search_metapackets, triples, result, child1, offset);
                            }

                            if (!inner_found)
                            {
                                if (null != child1)
                                    child1.Valid = false;

                                if (null != child2)
                                    child2.Valid = false;
                            }
                        }

                        found = found || inner_found;
                    }
                }

                //if (!found)
                noderesult.Valid = found;

                return found;
            }
        }

        public class NodeResult
        {
            public new string ToString()
            {
                return Utils.ToSimpleAddress(Address);
            }

            public string Simple
            {
                get { return Utils.ToSimpleAddress(Address); }
            }

            public string Name;

            public byte[] Address;

            public byte[] Hash;

            public List<NodeResult> Children = new List<NodeResult>();

            public Dictionary<string, List<NodeResult>> Matches = new Dictionary<string, List<NodeResult>>();

            public NodeResult Parent;

            bool _valid = true;

            public bool Valid
            {
                get
                {
                    return _valid;
                }
                set
                {
                    _valid = value;

                    if (null != Parent)
                        Parent.Valid = Parent.Children.Any(x => x.Valid);
                }
            }

            public NodeResult(NodeResult parent, string name, byte[] address, byte[] hash = null)
            {
                Parent = parent;

                Name = name;

                Address = address;

                Hash = hash;
            }
        }

        static void CleanResult(NodeResult result)
        {
            foreach (var r in result.Children)
            {
                CleanResult(r);
            }

            result.Children.RemoveAll(x => !x.Valid && (!x.Children.Any() || x.Children.All(y => !y.Valid)));

            foreach(var t in result.Children)
            {
                if (!result.Matches.ContainsKey(t.Name))
                    result.Matches.Add(t.Name, new List<NodeResult>());

                result.Matches[t.Name].Add(t);                    
            }
        }

        public static NodeResult Execute(string query, List<Metapacket> metapacket_list = null)
        {
            string t = null;
            string l = null;
            string a = "1354";



            //var list = metapacket_list.Where(x => (null == t || x.SimpleTargetAddress == t) && (null == l || x.SimpleTargetAddress == l) && (null == a || x.SimpleAddress == a)).ToArray();

            var rs = Triple.Parse(query);

            var result = new List<byte[]>(); 

            var nr = new NodeResult(null, ":ROOT", null);

            if (null == metapacket_list)
                metapacket_list = MetaPackets.Links.Items.Values
                .SelectMany(x => x).Concat(MetaPackets.Hashs.Items.Values
                .SelectMany(x => x))
                .ToList();

            Execute(metapacket_list, rs, result, nr);

            CleanResult(nr);



            return nr;
        }

        public static void PreSearch(List<Metapacket> metapacket_list)
        {
            var sql =
@"'sense84' :DIR-?-ROOT_SEQUENCE
:DIR-?-CONCEITO
:DIR-:INDEX-:TEMP
:INDEX-:INDEX_LINK-?
:INDEX_LINK-?-ORDER
:TEMP-?-CONCEITO
:TEMP-?-ROOT_SEQUENCE
:TEMP-?-:EPI
:EPI-?-ROOT_STREAM
:EPI-?-CONCEITO";

            sql =
@"'hhh' :DIR-?-ROOT_APP
:DIR-?-CONCEITO
:DIR-:INDEX-:FILE
:INDEX-:INDEX_LINK-?
:INDEX_LINK-?-ORDER
:FILE-?-CONCEITO
:FILE-:CONTENT_LINK-:T2#
:CONTENT_LINK-?-MIME_TYPE_DOWNLOAD";


            sql =
@"'hhh' :DIR-:INDEX-:FILE
:INDEX-:INDEX_LINK-?
:INDEX_LINK-?-ORDER
:FILE-?-CONCEITO
:FILE-:CONTENT_LINK-:CONTENT_ADDRESS#
:CONTENT_LINK-?-MIME_TYPE_DOWNLOAD";

            sql =
@"'hhh' :DIR-?-:FILE
:FILE-?-CONCEITO
:FILE-:CONTENT_LINK-:CONTENT_ADDRESS#
:CONTENT_LINK-?-MIME_TYPE_DOWNLOAD";


            //:CONTENT-?-MIME_TYPE_DOWNLOAD";

            //:FILE-?-CONCEITO
            //:FILE-?-DOWNLOAD
            //:FILE-?-:CONTENT
            //:CONTENT-?-LABEL
            //:CONTENT-?-INDEX";

            var rs = Triple.Parse(sql);

            var result = new List<byte[]>();


            var nr = new NodeResult(null, "ROOT", null);

            Execute(metapacket_list, rs, result, nr);

            CleanResult(nr);

            //var ss = t
            //    .Select(x => Utils.ToSimpleAddress(x)).Distinct().ToArray();    

            var s = "";
            foreach (var x in MetaPackets.Links.Items)
            {
                foreach (var y in x.Value)
                {
                    var linkAddress = Utils.ToSimpleAddress(y.LinkAddress);

                    var address = Utils.ToSimpleAddress(y.Address);

                    var targetAddress = Utils.ToSimpleAddress(y.TargetAddress);



                    if (y.Hash != null)
                    {
                        var p = Packets.Get(y.LinkAddress);

                        var packet = "null";

                        if (p != null)
                        {
                            packet = "PACOTE_" + ((p.Length - 21) / 2).ToString();

                            if (p.Length < 85)
                                packet = Encoding.UTF8.GetString(p.Skip(pParameters.packetHeaderSize).ToArray());

                        }

                        linkAddress = packet;
                    }

                    s += string.Format("{0}-{1}-{2}\r\n", targetAddress, address, linkAddress);

                }


                var bContextId = Utils.GetAddress();

                //    var sr = new SearchResult(bContextId, "fff\*\index.html\label", RenderMode.Main, new p2pContext());


            }
        }

    }
}
