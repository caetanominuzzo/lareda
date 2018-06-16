
using MathNet.Numerics.LinearAlgebra.Double;
using library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace sim2
{
    public partial class Form1 : Form
    {

        BackgroundWorker backgroundWorker = new BackgroundWorker();

        public static Dictionary<string, Point> cities = new Dictionary<string, Point>();

        public void citiesAdd(string s, Point p)
        {
            if (!cities.ContainsKey(s))
                cities.Add(s, p);
        }

        public static List<Peer> selected = new List<Peer>();
        public static List<Peer> selected2 = new List<Peer>();

        public static Bitmap bmpDHT = null;

        public static Bitmap bmpGeo = null;

        public static int GeoMax = 10000;

        public static double GeoMaxRatioLatency = 10000.0 / GeoMax;

        public static double GeoMaxDistance = Math.Sqrt(Math.Pow(GeoMax / 2, 2) * 2);

        public static int PeersMax = 5;

        public static int Id = 0;

        public static Random rand = new Random(Guid.NewGuid().GetHashCode());

        public static int Max = 10000;

        public class Peer
        {
            public int Id;

            public byte[] Address;

            public List<byte[]> Packets = new List<byte[]>();

            public Point Position;

            public Point GeoAddress;

            public List<Peer> Peers = new List<Peer>();

            public Dictionary<int, double> Px = new Dictionary<int, double>();

            internal double AvgDistancia = 0.5;

            internal double AvgGeoDistancia = 0.1;

            public Color Color;

            public Brush Brush;

            public Pen Pen;

            public string City;


            public override string ToString()
            {
                return Id + "-" + City;
            }

            public Peer(Peer parent = null)
            {
                Address = Utils.GetAddress();

                var keys = cities.Keys.ToArray();

                var city = rand.Next(cities.Count - 1);

                City = keys[city];

                GeoAddress = cities[keys[city]];// new Point(rand.Next(Form1.GeoMax), rand.Next(Form1.GeoMax));



                Id = Form1.Id++;

                if (null != parent)
                    Peers.Add(parent);

                Color = Color.FromArgb(Address[0], Address[1], Address[2]);

                Brush = new SolidBrush(Color);

                Pen = new Pen(Color);
            }
        }

        public List<Peer> Peers = new List<Peer>();

        public Form1()
        {
            InitializeComponent();

            LoadCities();

            this.DoubleBuffered = true;
            return;

            var qtd_peers = 500;

            AddPeers(500);

            // ProcessPeers();



            var qtd_processa = 10;

            var qtd_ciclos = 10;

            for (var i = 0; i < qtd_ciclos; i++)
            {
                //for (var j = 0; j < qtd_processa; j++)
                //    ProcessPeers();

                var r1 = NetAvgDistance();

                var r2 = NetAvgDistance(true);

                var r3 = NetAvQtdPeers();

                System.IO.File.AppendAllText("log.txt", Peers.Count() + "\t" + r1 + "\t" + r2 + "\t" + r3 + "\r\n");
            }


        }

        public double NetAvgDistance(bool Geo = false)
        {
            var avg = 0.0;

            foreach (var peer in Peers)
            {
                avg += (Geo ? peer.AvgGeoDistancia : peer.AvgDistancia) / Peers.Count();
            }

            return avg;
        }

        public double NetAvQtdPeers()
        {
            var avg = 0.0;

            foreach (var peer in Peers)
            {
                avg += (double)peer.Peers.Count() / Peers.Count();
            }

            return avg;
        }

        public void AddPeers(int qtd)
        {
            var newPeers = new List<Peer>();

            for (var i = 0; i < qtd; i++)
            {
                var peer = new Peer();

                newPeers.Add(peer);

                var peers = newPeers.OrderBy(x => Addresses.EuclideanDistance(peer.Address, x.Address)).Take(PeersMax);

                //foreach (var p in peers)
                //{
                //    if (!peer.Peers.Contains(p) && p != peer)
                //        peer.Peers.Add(p);
                //}

                Peers.Add(peer);

                if (i % 200 == 0)
                {
                    lblTotalPeers.Invoke(new TextDelegate(s => lblTotalPeers.Text = "Total Peers: " + s), Peers.Count().ToString());
                }
            }

            chkSelected.Invoke(new PeersDelegate(s =>
            {
                foreach (var peer in s)
                {
                    chkSelected.Items.Add(peer);
                }
            }), newPeers);

            chkChain.Invoke(new PeersDelegate(s =>
            {
                foreach (var peer in s)
                {
                    chkChain.Items.Add(peer);
                }
            }), newPeers);

            //svd();

            //PaintDht();

            //PaintDht(false);

        }

        public void svd()
        {
            var n = 2;

            var keys = Peers;

            var d = keys.Count();

            var mm = new double[d][];

            foreach (var k in keys)
            {
                var ikey = keys.IndexOf(k);

                var innerarray = new List<double>();

                foreach (var kk in keys)
                {
                    innerarray.Add(Addresses.EuclideanDistance(keys[ikey].Address, kk.Address));
                }

                mm[ikey] = innerarray.ToArray();// keys[ikey].Address.Select(x=>(double)x).ToArray();// innerArray.ToArray();
            }

            var m = DenseMatrix.OfColumnArrays(mm);

            var svd = m.Svd(true);
            //var svd = new UserSvd(m, true);

            var w = svd.W.ToColumnArrays();
            var vt = svd.VT.ToColumnArrays();

            var m3 = DenseMatrix.OfColumnArrays(w);

            var m4 = m3 * svd.VT;

            var m5 = m4.NormalizeRows(2).ToColumnArrays();

            var xises = new double[m5[0].Length];
            var ypes = new double[m5[0].Length];

            for (var k = 0; k < m5[0].Length; k++)
            {
                xises[k] = m5[k][0];
                ypes[k] = m5[k][1];
            }

            var xmin = xises.Min();
            var xmax = xises.Max();

            var ymin = ypes.Min();
            var ymax = ypes.Max();

            for (var k = 0; k < m5[0].Length; k++)
            {
                xises[k] = (xises[k] - xmin) / (xmax - xmin);
                ypes[k] = (ypes[k] - ymin) / (ymax - ymin);
            }

            for (var k = 0; k < m5[0].Length; k++)
            {
                keys[k].Position = new Point((int)(xises[k] * Max), (int)(ypes[k] * Max));
            }


            foreach (var i in m5)
            {

            }
        }

        public void ProcessPeers(int qtd)
        {
            for (var i = 0; i < qtd; i++)
            {
                AddPeers(int.Parse(txtAddPeers.Text));


                var t = Peers.Where(x => double.IsNaN(x.AvgDistancia) || double.IsNaN(x.AvgGeoDistancia)).ToArray();

                if (t.Length > 0)
                {

                }

                //lock (Peers)
                //    Peers.RemoveAll(x => double.IsNaN(x.AvgDistancia) || double.IsNaN(x.AvgGeoDistancia));

                var handles = new List<ManualResetEvent>();


                foreach (var peer in Peers.OrderBy(x => rand.NextDouble()))//  x.Peers.Count))
                {
                    var handle = new ManualResetEvent(false);

                    if (handles.Count > 60)
                    {
                        WaitHandle.WaitAll(handles.ToArray());

                        handles.Clear();
                    }

                    handles.Add(handle);



                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            if (peer.Id == 14)
                            {

                            }
                            var closest = peer.Peers.OrderBy(x => rand.Next()).FirstOrDefault();

                            Peer result = null;

                            //var closest = Search(peer, peer.Address, ref result);

                            if (null != closest)
                            {
                                var closests_from_closest = closest.Peers.OrderBy(x => Addresses.EuclideanDistance(peer.Address, x.Address)).Take(PeersMax);//  ).Take(100);Addresses.EuclideanDistance(peer.Address, x.Address))

                                foreach (var c in closests_from_closest)
                                {
                                    if (c != peer)
                                    {
                                        lock (c.Peers)
                                            foreach (var pp in c.Peers)
                                            {
                                                if (pp != c && peer != pp)
                                                {
                                                    if (!peer.Peers.Contains(pp) && peer != pp)
                                                        lock (peer.Peers)
                                                            peer.Peers.Add(pp);

                                                    //if (!pp.Peers.Contains(peer) && peer != pp)
                                                    //    pp.Peers.Add(peer);
                                                }
                                            }

                                        break;
                                    }
                                }
                            }

                            peer.AvgDistancia = AvgDistance(peer);

                            peer.AvgGeoDistancia = AvgDistance(peer, true);

                            var perc_to_remove = (peer.Peers.Count() - PeersMax) / (double)peer.Peers.Count();// .9;

                            var total = peer.Peers.Count();

                            var total_to_remove = total * perc_to_remove;

                            var left = total - total_to_remove;

                            if (total_to_remove < 1)
                                return;

                            while (peer.Peers.Count() > PeersMax)
                            {
                                var toRemove = new List<Peer>();

                                foreach (var other_peer in peer.Peers.OrderBy(x => rand.Next()))
                                {
                                    var pT = ((double)total - left) / total;

                                    var pL = Math.Log(100 * (Addresses.EuclideanDistance(peer.Address, other_peer.Address)), (peer.AvgDistancia * 100)) - 1;

                                    var pLS = Math.Log(100 * (Distance(peer.GeoAddress, other_peer.GeoAddress)), (peer.AvgGeoDistancia * 100)) - 1;

                                    //var pL1 = Math.Log(100 * (Distance(peer.Address, other_peer.SemanticAddress) / Max), (perc_avg * 100)) - 1;

                                    //var pLS1 = Math.Log(100 * (Distance(peer.SemanticAddress, other_peer.Address) / Max), (perc_avg * 100)) - 1;

                                    if (peer.Px.ContainsKey(other_peer.Id))
                                        peer.Px[other_peer.Id] = Math.Min(pL, pLS);
                                    else
                                        peer.Px.Add(other_peer.Id, Math.Min(pL, pLS));

                                    //other_peer.px = Math.Min(pL, pLS);// (pT + Math.Min(Math.Min(Math.Min(pL, pLS), pLS), pLS) *.25) / 1.25;

                                    //                            other_peer.px = (100 * pL + pLS) / 101.0;

                                    //other_peer.px = (pL * 2 + pLS * 1) / 3;
                                    //if (pT > 0 && library.Utils.Roll(px))
                                    //{
                                    //    toRemove.Add(other_peer);

                                    //    total--;
                                    //}
                                    //else total++;

                                }
                                //var sum_px = peer.Peers.Sum(x => x.px);
                                //foreach (var other_peer in peer.Peers.OrderBy(x => rand.Next()))
                                //{
                                //    other_peer.px = other_peer.px / sum_px;

                                //    if (library.Utils.Roll(other_peer.px))
                                //    {
                                //        toRemove.Add(other_peer);

                                //        total--;
                                //    }
                                //}


                                toRemove.AddRange(peer.Peers.OrderByDescending(x => peer.Px[x.Id]).Take(Math.Max(0, peer.Peers.Count() - PeersMax)));

                                lock (peer.Peers)
                                    peer.Peers.RemoveAll(x => toRemove.Any(y => y == x));
                            }
                        }
                        finally
                        {
                            handle.Set();
                        }
                    });


                }

                AddSeries();

                //if (tabControl1.SelectedIndex == 1)
                //{
                //    //svd();

                //    // PaintDht();

                //    // PaintDht(false);
                //}
                //Application.DoEvents();
            }
        }

        public class Data
        {
            public double NetAvgDistance;

            public double NetAvgGeoDistance;

            public double QtdPeers;

            public double QtdJumps;

            public double ReturnRatio;

            public double Latency;
        }

        public delegate void DataDelegate(Data data);
        public delegate void TextDelegate(string text);

        public delegate void PeerDelegate(string peer);
        public delegate void PeersDelegate(IEnumerable<Peer> peer);

        void AddSeries()
        {
            var data = new Data();

            data.NetAvgDistance = NetAvgDistance();

            data.NetAvgGeoDistance = NetAvgDistance(true);

            data.QtdPeers = NetAvQtdPeers();

            data.QtdJumps = AvgJumps(out data.ReturnRatio, out data.Latency);

            var handle = new ManualResetEvent(false);

            BackgroundWorker bg = new BackgroundWorker();

            chart1.Invoke(new DataDelegate(d =>
            {
                chart1.Series[0].Points.AddY(Math.Max(0, Math.Min(1, data.NetAvgDistance)));

                chart1.Series[1].Points.AddY(Math.Max(0, Math.Min(1, data.NetAvgGeoDistance)));

                chart1.Series[2].Points.AddY(data.QtdPeers);

                chart1.Series[3].Points.AddY(Math.Max(0, Math.Min(1, (Math.Log(Peers.Count()) / Math.Log(data.QtdPeers)) / data.QtdJumps)));

                chart1.Series[4].Points.AddY(Math.Max(0, Math.Min(1, data.ReturnRatio)));

                chart1.Series[5].Points.AddY(Math.Max(0, Math.Min(1, data.Latency * 100)));

                chart2.Series[0].Points.AddY(Peers.Count());

                System.IO.File.AppendAllText("log.txt",
                    data.NetAvgDistance + "\t" +
                    data.NetAvgGeoDistance + "\t" +
                    data.QtdPeers + "\t" +
                     ((Math.Log(Peers.Count()) / Math.Log(data.QtdPeers)) / data.QtdJumps) + "\t" +

                    data.ReturnRatio + "\t" +
                    data.Latency + "\t" +
                    Peers.Count() + "\r\n");

                Application.DoEvents();

                handle.Set();
            }), data);

            handle.WaitOne();
        }

        List<Peer> Find(byte[] target, List<Peer> chain)
        {
            if (chain.Count() > 50)
                return chain;

            var last = chain.Last();

            if (Addresses.Equals(last.Address, target))
                return chain;



            var closests = last.Peers.OrderBy(x => Addresses.EuclideanDistance(x.Address, target)).Take(Math.Max(1, 1));// (int)Math.Log(chain.Count)));

            if (closests.Count() == 0)
                return chain;

            var closest = closests.OrderBy(x => Distance(x.GeoAddress, last.GeoAddress)).Take(Math.Max(1, (int)Math.Log(chain.Count / 2.0))).OrderBy(x => Addresses.EuclideanDistance(x.Address, target)).FirstOrDefault();

            if (null == closest)
                return chain;

            var dthis = Addresses.EuclideanDistance(last.Address, target);

            var cthis = Addresses.EuclideanDistance(closest.Address, target);

            if (dthis < cthis)
            {
                // closest = last.Peers.OrderBy(x => rand.NextDouble()).FirstOrDefault();

                //closest = closests.OrderBy(x => Distance(x.GeoAddress, last.GeoAddress)).Take(Math.Max(1, (int)Math.Log(chain.Count / 2.0))).OrderBy(x => Addresses.EuclideanDistance(x.Address, target)).FirstOrDefault();

                if (null == closest)
                    return chain;

                chain.Add(closest);

                return Find(target, chain);
            }

            chain.Add(closest);

            return Find(target, chain);
        }

        double AvgJumps(out double rate, out double latency)
        {
            var qtd = 100;

            List<Peer> longest = new List<Peer>();

            var sum = 0.0;

            var sum_latency = 0.0;

            var foundChains = 0.00;

            for (var i = 0; i < qtd; i++)
            {
                //Text = i.ToString();

                var p1 = Peers[rand.Next(Peers.Count)];

                rand = new Random(p1.Id);

                var p2 = Peers[rand.Next(Peers.Count)];

                //p2 = Peers.OrderBy(x => rand.NextDouble()).First();

                var Chain = new List<Peer>();

                Chain.Add(p1);

                Chain = Find(p2.Address, Chain);

                if (Addresses.Equals(Chain.Last().Address, p2.Address))
                {
                    sum += Chain.Count();

                    for (var j = 0; j < Chain.Count() - 2; j++)
                        sum_latency += Distance(Chain[j].GeoAddress, Chain[j + 1].GeoAddress) * GeoMaxRatioLatency;

                    foundChains++;

                    if (Chain.Count() > longest.Count())
                        longest = Chain.ToList();
                }
                else
                {

                }
            }

            var r = sum / foundChains;

            var sumPeers = 0.0;

            foreach (var peer in Peers)
            {
                sumPeers += peer.Peers.Count();
            }

            sumPeers /= Peers.Count();

            rate = foundChains / qtd;

            latency = sum_latency / foundChains;

            return r;
        }

        private static double AvgDistance(Peer peer, bool Geo = false)
        {
            double peers_dist_sum = 0;

            double peers_dist_count = 0;

            double max_avg = (PeersMax / 2.0);

            foreach (var other_peer in peer.Peers.OrderBy(x => Geo ? Distance(peer.GeoAddress, x.GeoAddress) : Addresses.EuclideanDistance(peer.Address, x.Address)))
            {
                peers_dist_sum += Geo ?
                    Distance(peer.GeoAddress, other_peer.GeoAddress) :
                    Addresses.EuclideanDistance(peer.Address, other_peer.Address);

                if (peers_dist_count++ > max_avg)
                    break;
            }

            var avg_peer_dist = (double)peers_dist_sum / peers_dist_count;

            var perc_avg = avg_peer_dist;// / (Geo ? GeoMaxDistance : pParameters.MaxDistance);

            return perc_avg;
        }

        public static double Distance(Point xx, Point yy)
        {
            double sum = 0;

            var x = new int[] { xx.X, xx.Y };

            var y = new int[] { yy.X, yy.Y };

            for (var i = 0; i < x.Length; i++)
            {
                double diff = Math.Abs(x[i] - y[i]);

                sum += Math.Pow(Math.Min(diff, GeoMax - diff), 2);//toroidal

                //sum += Math.Pow(diff, 2);
            }

            sum = Math.Sqrt(sum);

            return sum / GeoMaxDistance;
        }

        private void btnAddPeers_Click(object sender, EventArgs e)
        {
            AddPeers(int.Parse(txtAddPeers.Text));
            /*
            svd();

            picDHT.Invalidate();

            picGeo.Invalidate();

        */
        }

        private Point DrawBezier(Peer peer, Peer target, int ratio_x, int ratio_y, Point p1, Point p2, Graphics g = null, bool justCalc = false, bool chain = false, Pen chainpen = null)
        {
            var alpha = chkSelected.CheckedItems.Count == 0 ||
                chkSelected.CheckedItems.Contains(peer.Id) ?
                100 :
                50;

            var pen = new Pen(Color.FromArgb(alpha, peer.Color), 2);

            if (null != chainpen)
                pen = chainpen;

            var diff_x_straight = Math.Abs(p2.X - p1.X);

            var diff_y_straight = Math.Abs(p2.Y - p1.Y);

            var diff_x_counter = p1.X - p2.X;

            var diff_y_counter = p1.Y - p2.Y;

            var max_x = Max / ratio_x;

            var max_y = Max / ratio_y;

            var p3 = p2;

            if (justCalc)
            {
                if (Math.Abs(diff_x_straight) > Max / 2)// < Math.Abs(diff_x_counter - Max))
                {
                    if (diff_x_counter < 0)
                        p3.X = p3.X - Max;
                    else
                        p3.X = p3.X + Max;

                }

                if (Math.Abs(diff_y_straight) > Max / 2)// < Math.Abs(diff_y_counter - Max))
                {
                    if (diff_y_counter < 0)
                        p3.Y = p3.Y - Max;
                    else
                    {
                        p3.Y = p3.Y + Max;
                    }
                }
                return p3;
            }


            if (chkTorus.Checked && (diff_x_straight > max_x / 2 || diff_y_straight > max_y / 2))
            {
                if (diff_x_straight > max_x / 2)
                {
                    if (diff_x_counter < 0)
                    {
                        p3.X = p3.X - max_x;
                        p1.X = p1.X + max_x;
                    }
                    else
                    {
                        p1.X = p1.X - max_x;
                        p3.X = max_x + p3.X;
                    }
                }

                if (diff_y_straight > max_y / 2)
                {
                    if (diff_y_counter < 0)
                    {
                        p1.Y = p1.Y + max_y;
                        p3.Y = p3.Y - max_y;
                    }
                    else
                    {
                        p1.Y = p1.Y - max_y;
                        p3.Y = max_y + p3.Y;
                    }
                }

                //e.Graphics.DrawLine(peer.Pen, p1.X, p1.Y, p2.X, p2.Y);

                var p11 = new Point(((p1.X + p2.X) / 2) + 20, ((p1.Y + p2.Y) / 2) + 20);

                var p22 = new Point(((p1.X + p2.X) / 2) - 20, ((p1.Y + p2.Y) / 2) - 20);



                if (!justCalc)
                    g.DrawBezier(pen, p1, p11, p22, p2);

                if (p1.X > max_x)
                {
                    p2.X = p2.X - max_x;
                    p1.X = p1.X - max_x;
                }
                else if (p1.X < 0)
                {
                    p2.X = p2.X + max_x;
                    p1.X = p1.X + max_x;
                }

                if (p1.Y > max_y)
                {
                    p2.Y = p2.Y - max_y;
                    p1.Y = p1.Y - max_y;
                }
                else if (p1.Y < 0)
                {
                    p2.Y = p2.Y + max_y;
                    p1.Y = p1.Y + max_y;
                }

                if (justCalc)
                    return p3;

            }


            //e.Graphics.DrawLine(peer.Pen, p1.X, p1.Y, p2.X, p2.Y);

            var p111 = new Point(((p1.X + p2.X) / 2) + 20, ((p1.Y + p2.Y) / 2) + 20);

            var p222 = new Point(((p1.X + p2.X) / 2) - 20, ((p1.Y + p2.Y) / 2) - 20);

            if (!justCalc)
                g.DrawBezier(pen, p1, p111, p222, p2);

            return p2;
        }

        private void btNClearPeers_Click(object sender, EventArgs e)
        {
            Peers.Clear();

            lblTotalPeers.Text = "Total Peers: " + Peers.Count();
        }

        private void btNProcess_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                ProcessPeers(int.Parse(txtProcessPeers.Text));
            });
        }

        private void txtProcessPeers_TextChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            picDHT.Invalidate();
        }


        public void PaintDht(bool dht = true)
        {
            try
            {
                if (dht)
                    bmpDHT = new Bitmap(picDHT.Width, picDHT.Height);
                else
                    bmpGeo = new Bitmap(picGeo.Width, picGeo.Height);

                var g = Graphics.FromImage(dht ? bmpDHT : bmpGeo);

                g.Clear(Color.Black);

                var mx = 10000;
                var mX = -1;

                var my = 10000;
                var mY = -1;

                var ratio_x = Max / (dht ? picDHT.Width : picGeo.Width);
                var ratio_y = Max / (dht ? picDHT.Height : picGeo.Height);

                lock (Peers)
                    foreach (var p in Peers)
                    {
                        if (p.Position.X > mX)
                            mX = p.Position.X;

                        if (p.Position.X < mx)
                            mx = p.Position.X;

                        if (p.Position.X > mY)
                            mY = p.Position.Y;

                        if (p.Position.X < my)
                            mY = p.Position.Y;

                        g.FillEllipse(p.Brush, (dht ? p.Position.X : p.GeoAddress.X) / ratio_x, (dht ? p.Position.Y : p.GeoAddress.Y) / ratio_y, 10, 10);

                        foreach (var pp in p.Peers)
                        {
                            DrawBezier(p, pp, (int)ratio_x, ratio_y,
                                new Point((int)((dht ? p.Position.X : p.GeoAddress.X) / ratio_x), ((dht ? p.Position.Y : p.GeoAddress.Y) / ratio_y)),
                                new Point((int)((dht ? pp.Position.X : pp.GeoAddress.X) / ratio_x), ((dht ? pp.Position.Y : pp.GeoAddress.Y) / ratio_y)), g);
                        }
                    }

                if (chkChain.CheckedItems.Count == 0)
                    foreach (var p in Peers)
                    {

                        g.DrawString(p.ToString(), SystemFonts.CaptionFont, new SolidBrush(Color.White), (int)((dht ? p.Position.X : p.GeoAddress.X) / ratio_x), ((dht ? p.Position.Y : p.GeoAddress.Y) / ratio_y));
                    }

                if (dht)
                    picDHT.Image = bmpDHT;
                else
                    picGeo.Image = bmpGeo;

                foreach (Peer p in chkSelected.CheckedItems)
                {
                    g.DrawEllipse(p.Pen, (dht ? p.Position.X : p.GeoAddress.X) / ratio_x, (dht ? p.Position.Y : p.GeoAddress.Y) / ratio_y, 10, 10);
                }


                foreach (Peer p in chkSelected.CheckedItems)
                {
                    g.DrawString(p.City.ToString(), SystemFonts.CaptionFont, new SolidBrush(Color.White), (int)((dht ? p.Position.X : p.GeoAddress.X) / ratio_x), ((dht ? p.Position.Y : p.GeoAddress.Y) / ratio_y));
                }


                if (chkChain.CheckedItems.Count > 0)
                {
                    foreach (Peer i in chkSelected.CheckedItems)
                        foreach (Peer j in chkChain.CheckedItems)
                        {
                            var chain = new List<Peer>();

                            chain.Add(i);

                            chain = Find(j.Address, chain);

                            var chainpen = new Pen(Color.FromArgb(150, chain[0].Color), 2);

                            for (var k = 0; k < chain.Count - 1; k++)
                            {
                                var p = chain[k];

                                var pp = chain[k + 1];

                                DrawBezier(p, pp, (int)ratio_x, ratio_y,
                                    new Point((int)((dht ? p.Position.X : p.GeoAddress.X) / ratio_x), ((dht ? p.Position.Y : p.GeoAddress.Y) / ratio_y)),
                                    new Point((int)((dht ? pp.Position.X : pp.GeoAddress.X) / ratio_x), ((dht ? pp.Position.Y : pp.GeoAddress.Y) / ratio_y)), g, false, false, chainpen);
                            }
                        }
                }



            }
            catch (Exception ex)
            {

            }
        }

        private void chkTorus_CheckedChanged(object sender, EventArgs e)
        {
            //    svd();

            PaintDht();

            PaintDht(false);
        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            if (panel1.Height == 123)
                panel1.Height = 5;
            else
                panel1.Height = 123;
        }

        private void btnOptimize_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(oo =>
            {

                var cacheDht = new Dictionary<string, double>();

                var handles = new List<ManualResetEvent>();

                var i = 0;
                foreach (var peer in Peers)
                {
                    if (peer.Peers.Count > 0)
                        continue;

                    var handle = new ManualResetEvent(false);

                    if (handles.Count > 60)
                    {
                        WaitHandle.WaitAll(handles.ToArray());

                        handles.Clear();
                    }

                    handles.Add(handle);

                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            peer.Peers.AddRange(Peers.OrderBy(x => rand.Next()
                            /*
                            .OrderByDescending(other_peer =>
                            {

                            var ddht = 0.0;

                            ddht = Addresses.EuclideanDistance(peer.Address, other_peer.Address);

                                var pL = Math.Log(100 * (ddht), (peer.AvgDistancia * 100)) - 1;

                                var pLS = Math.Log(100 * (Distance(peer.GeoAddress, other_peer.GeoAddress)), (peer.AvgGeoDistancia * 100)) - 1;


                             var px = Math.Min(pL, pLS);// (pT + Math.Min(Math.Min(Math.Min(pL, pLS), pLS), pLS) *.25) / 1.25;


                            return px;
                            }
                            */
                            ).Take(PeersMax));

                            peer.AvgDistancia = AvgDistance(peer);

                            peer.AvgGeoDistancia = AvgDistance(peer, true);


                        }
                        finally {
                            handle.Set();
                        }
                    });

                    this.Invoke(new TextDelegate(s => Text = s), i++.ToString());

                }

                WaitHandle.WaitAll(handles.ToArray());

                ProcessPeers(int.Parse(txtProcessPeers.Text));
            });

            PaintDht();

            PaintDht(false);

        }

        private void btnSeries_Click(object sender, EventArgs e)
        {
            AddSeries();
        }

        private void txtLinks_TextChanged(object sender, EventArgs e)
        {
            int i = 0;
            if (int.TryParse(txtLinks.Text, out i))
            {
                PeersMax = i;

            }
        }
    }
}
