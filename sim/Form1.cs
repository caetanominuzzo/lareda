using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace sim
{
    public partial class Form1 : Form
    {
        internal static int Max = 10120;

        static Random rand = new Random();

        internal static int Id = 0;

        internal Peer selectedPeer = null;

        internal bool onlyCircles = false;

        internal static Point Rand()
        {
            return new Point(rand.Next(Max), rand.Next(Max));
        }

        public class Peer
        {
            public Point Address;

            public Point SemanticAddress;

            public List<Peer> Peers = new List<Peer>();

            public List<Point> Packets = new List<Point>();

            public Color PeerColor = Color.FromArgb(255, rand.Next(256), rand.Next(256), rand.Next(256));

            public Brush Brush;

            public Brush PacketBrush;

            public Pen Pen;

            public int Id;

            public double AvgDistancia = 0;

            public double px = 0.0;

            public Peer(Point address, IEnumerable<Peer> parent = null)
            {
                Address = address;



                PeerColor = Color.FromArgb(255, Form1.colors.GetPixel(Address.X * 100 / Max, Address.Y * 100 / Max));

                Brush = new SolidBrush(PeerColor);

                var pColor = Color.FromArgb(30, PeerColor);

                PacketBrush = new SolidBrush(pColor);

                Pen = new Pen(pColor, 3);

                if (null != parent)
                {
                    foreach (var p in parent)
                    {
                        Peers.Add(p);

                        p.Peers.Add(this);
                    }
                }

                Id = Form1.Id++;

                //if(Id == 0)
                //    SemanticAddress = new Point(1000,1000);
                //else if (Id == 1)
                //    SemanticAddress = new Point(7000, 7000);
                //else
                SemanticAddress = Rand();

                //SemanticAddress = Address;
            }
        }

        public static Bitmap colors;

        public Form1()
        {
            InitializeComponent();

            colors = new Bitmap("colors.bmp");

            Dictionary<double, int> d = new Dictionary<double, int>();

            d.Add(123, 1);
            d.Add(1, 1);
            d.Add(1233, 1);
            d.Add(2, 1);

            var i = d.OrderBy(x => x.Key).Take(3).ToArray();
        }

        public List<Peer> Peers = new List<Peer>();

        Point Closest(Peer peer, Point target)
        {
            var p1 = peer.Address;

            var p2 = peer.SemanticAddress;

            var p1d = Distance(p1, target);

            var p2d = Distance(p2, target);

            return p1d < p2d ? p1 : p1;
        }

        List<Peer> Find(Point target, List<Peer> chain)
        {
            if (chain.Count() > 100)
                return chain;
            var last = chain.Last();

            if (last.Address == target)
                return chain;

            var dthis = Distance(Closest(last, target), target);

            var closest = last.Peers.OrderBy(x => Distance(Closest(x, target), target)).FirstOrDefault();

            if (null == closest)
                return Chain;

            var cthis = Distance(Closest(closest, target), target);

            if (dthis < cthis)
            {
               // closest = last.Peers.OrderBy(x => rand.NextDouble()).FirstOrDefault();

                if (null == closest)
                    return Chain;

                chain.Add(closest);

                return Find(target, chain);
            }

            chain.Add(closest);

            return Find(target, chain);
        }

        List<Peer> Chain = null;

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 't')
            {

                Peers.RemoveAll(x => x.AvgDistancia == double.NaN);

                for (var qtd_peers = 1; qtd_peers < 50000; qtd_peers += 1000)
                {
                    //                    e.KeyChar = 'a';

                    //Form1_KeyPress(sender, e);

                    for (var processa = 0; processa < 0; processa++)
                    {
                        e.KeyChar = 'p';

                        //Form1_KeyPress(sender, e);
                    }

                    List<Peer> longest = new List<Peer>();

                    var sum = 0.0;


                    var foundChains = 0.00;

                    for (var i = 0; i < 1000; i++)
                    {
                        var p1 = Peers.OrderBy(x => rand.NextDouble()).First();

                        var p2 = Peers.OrderBy(x => rand.NextDouble()).First();

                        Chain = new List<Peer>();

                        Chain.Add(p1);

                        Chain = Find(p2.Address, Chain);

                        if (Chain.Last().Address == p2.Address || Chain.Last().SemanticAddress == p2.Address)
                        {
                            sum += Chain.Count();
                            foundChains++;

                            if (Chain.Count() > longest.Count())
                                longest = Chain.ToList();
                        }
                        else
                        {

                        }


                    }

                    var r = sum / foundChains;

                    Chain = longest.ToList();

                    var sumPeers = 0.0;
                    foreach (var peer in Peers)
                    {
                        sumPeers += peer.Peers.Count();
                    }

                    sumPeers /= Peers.Count();

                    System.IO.File.AppendAllText("log.txt", Peers.Count() + "\t" + sumPeers + "\t" + r + "\t" + (1000 - foundChains) + "\r\n");
                    pictureBox1.Invalidate();
                    return;
                }



            }

            if (e.KeyChar == 'c')
            {
                Peers.Clear();

                Form1.Id = 0;
            }

            if (e.KeyChar == 'o')
            {
                onlyCircles = !onlyCircles;
            }


            if (e.KeyChar == 'A')
            {
                for (var x = 0; x < Max; x+=Max/10)
                    for (var y = 0; y < Max; y += Max / 10)
                    {
                        var peer = new Peer(new Point(x+(Max / 20),y + (Max / 20)), Peers.Take(Math.Max(3, 3)));

                        Peers.Add(peer);

                        e.KeyChar = 'p';

                        //for (var k = 0; k < 10; k++)
                        Form1_KeyPress(sender, e);
                    }
            }

            if (e.KeyChar == 'a')
            {
                for (var jj = 0; jj < 50; jj++)
                {
                    //var peer = new Peer(Rand(), Peers.Take(Math.Max(3, 3)));

                    //Peers.Add(peer);

                    e.KeyChar = 'p';

                    //for (var k = 0; k < 10; k++)
                    Form1_KeyPress(sender, e);
                }
            }

            if (e.KeyChar == 'j')
            {
                var ratio_x = Max / pictureBox1.Width;
                var ratio_y = Max / pictureBox1.Height;

                var list = Peers.OrderBy(x => rand.NextDouble()).ToList();

                foreach (var p in list)
                {
                    var sum_x = (double)p.Address.X;
                    var sum_y = (double)p.Address.Y;

                    var peers = p.Peers.OrderBy(x => Distance(x.SemanticAddress, p.SemanticAddress)).Take(p.Peers.Count / 3);

                    foreach (var pp in peers)
                    {
                        var p2 = DrawBezier(p, ratio_x, ratio_y, p.SemanticAddress, pp.SemanticAddress, null, true);

                        if (p2.X > Max)
                            p2.X = Max - p2.X;
                        if (p2.X < 0)
                            p2.X += Max;

                        if (p2.Y > Max)
                            p2.Y = Max - p2.Y;
                        if (p2.Y < 0)
                            p2.Y += Max;

                        sum_x += p2.X;
                        sum_y += p2.Y;
                    }

                    sum_x /= (peers.Count() + 1);
                    sum_y /= (peers.Count() + 1);



                    var neo = new Point((int)sum_x, (int)sum_y);

                    p.SemanticAddress = neo;
                }
            }

            if (e.KeyChar == 'b')
            {
                foreach (var peer in Peers)
                {
                    for (var i = 0; i < 10; i++)
                        peer.Packets.Add(Rand());
                }
            }

            if (e.KeyChar == 'w')
            {
                foreach (var peer in Peers)
                {
                    var perc_to_remove = 0.1;

                    var total = peer.Packets.Count();

                    var total_to_remove = total * perc_to_remove;

                    var left = total - total_to_remove;

                    if (total_to_remove < 1)
                        continue;

                    var toRemove = new List<Point>();

                    foreach (var packet in peer.Packets.OrderBy(x => rand.Next()))
                    {
                        var pT = ((double)total - left) / total;

                        var pL = Math.Log(100 * (Distance(peer.Address, packet) / Max), peer.AvgDistancia * 100) - 1;

                        if (pT > 0 && library.Utils.Roll((pT + pL * 1) / 2))
                        {
                            toRemove.Add(packet);

                            var closest = peer.Peers.OrderBy(x => Distance(x.Address, packet)).FirstOrDefault();

                            closest.Packets.Add(packet);

                            total--;
                        }

                    }

                    peer.Packets.RemoveAll(x => toRemove.Any(y => y == x));
                }
            }
            if (e.KeyChar == 'p')
            {
                foreach (var peer in Peers)
                {

                    var closest = peer.Peers.OrderBy(x => rand.Next()).FirstOrDefault();

                    Peer result = null;

                    //var closest = Search(peer, peer.Address, ref result);

                    if (null != closest)
                    {
                        var closests_from_closest = closest.Peers.OrderBy(x => rand.Next()).Take(1);

                        foreach (var c in closests_from_closest)
                        {
                            if (c != peer)
                            {
                                if (!peer.Peers.Any(x => Distance(x.Address, c.Address) == 0))
                                {
                                    peer.Peers.Add(c);

                                    c.Peers.Add(peer);
                                }
                            }
                        }
                    }

                    var peers_dist_sum = 0;

                    var peers_dist_count = 0;

                    var max_avg = peer.Peers.Count() / 3;

                    foreach (var other_peer in peer.Peers)
                    {
                        peers_dist_sum += (int)Distance(peer.Address, other_peer.Address);

                        if (peers_dist_count++ > max_avg)
                            break;
                    }

                    var avg_peer_dist = (double)peers_dist_sum / peers_dist_count;

                    var perc_avg = avg_peer_dist / Max;

                    peer.AvgDistancia = perc_avg;

                    var mmax = 50;

                    var perc_to_remove = (peer.Peers.Count() - mmax) / (double)peer.Peers.Count();// .9;

                    var total = peer.Peers.Count();

                    var total_to_remove = total * perc_to_remove;

                    var left = total - total_to_remove;

                    if (total_to_remove < 1)
                        continue;

                    while (peer.Peers.Count() > mmax)
                    {
                        var toRemove = new List<Peer>();

                        foreach (var other_peer in peer.Peers.OrderBy(x => rand.Next()))
                        {
                            var pT = ((double)total - left) / total;

                            var pL = Math.Log(100 * (Distance(peer.Address, other_peer.Address) / Max), (perc_avg * 100)) - 1;

                            var pLS = Math.Log(100 * (Distance(peer.SemanticAddress, other_peer.SemanticAddress) / Max), (perc_avg * 100)) - 1;

                            //var pL1 = Math.Log(100 * (Distance(peer.Address, other_peer.SemanticAddress) / Max), (perc_avg * 100)) - 1;

                            //var pLS1 = Math.Log(100 * (Distance(peer.SemanticAddress, other_peer.Address) / Max), (perc_avg * 100)) - 1;

                            other_peer.px = Math.Max(pL, pLS);// (pT + Math.Min(Math.Min(Math.Min(pL, pLS), pLS), pLS) *.25) / 1.25;

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

                        toRemove.AddRange(peer.Peers.OrderByDescending(x => x.px).Take(peer.Peers.Count() - mmax));



                        peer.Peers.RemoveAll(x => toRemove.Any(y => y == x));
                    }
                }

            }

            pictureBox1.Invalidate();

            //pictureBox2.Invalidate();


        }

        Peer Search(Peer peer, Point target, ref Peer result)
        {
            foreach (var p in peer.Peers)
            {
                var order = p.Peers.OrderBy(x => Distance(x.Address, target));

                var closest = p.Peers.FirstOrDefault(x => Distance(x.Address, target) < Distance(peer.Address, target));

                if (null == closest)
                {
                    if (result == null)
                        return peer;

                    return result;
                }

                result = closest;

                return Search(closest, target, ref result);
            }

            return null;
        }

        public static double Distance(Point xx, Point yy)
        {
            double sum = 0;

            var x = new int[] { xx.X, xx.Y };

            var y = new int[] { yy.X, yy.Y };

            for (var i = 0; i < x.Length; i++)
            {
                double diff = Math.Abs(x[i] - y[i]);

                sum += Math.Pow(Math.Min(diff, Max - diff), 2);//toroidal

                //sum += Math.Pow(diff, 2);
            }

            sum = Math.Sqrt(sum);

            return sum;
        }


        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.Black), e.ClipRectangle);

            var c = (Control)sender;

            foreach (var peer in Peers)
            {
                DrawPeer(e, peer, c.Name.EndsWith("2"));
            }

            if (null != selectedPeer)
                DrawPeer(e, selectedPeer, c.Name.EndsWith("2"));

            var ratio_x = Max / pictureBox1.Width;
            var ratio_y = Max / pictureBox1.Height;

            if (null != Chain)
            {
                for (var i = 0; i < Chain.Count() - 1; i++)
                {
                    var p1 = Chain[i];
                    var p2 = Chain[i + 1];

                    var address = p1.Address;
                    var other_address = p2.Address;

                    if (c.Name.EndsWith("2"))
                    {
                        address = p1.SemanticAddress;

                        other_address = p2.SemanticAddress;
                    }

                    var p11 = new Point(address.X / ratio_x, address.Y / ratio_y);

                    var p22 = new Point(other_address.X / ratio_x, other_address.Y / ratio_y);

                    DrawBezier(p1, ratio_x, ratio_y, p11, p22, e, false, true);

                    e.Graphics.FillEllipse(new SolidBrush(Color.Red), (address.X / ratio_x) - 5, (address.Y / ratio_y) - 5, 10, 10);

                }

                var pp1 = Chain.First();
                var pp2 = Chain.Last();

                var paddress = pp1.Address;
                var pother_address = pp2.Address;

                if (c.Name.EndsWith("2"))
                {
                    paddress = pp1.SemanticAddress;

                    pother_address = pp2.SemanticAddress;
                }

                var pp11 = new Point(paddress.X / ratio_x, paddress.Y / ratio_y);

                var pp22 = new Point(pother_address.X / ratio_x, pother_address.Y / ratio_y);


                e.Graphics.DrawEllipse(new Pen(Color.Blue, 3), (paddress.X / ratio_x) - 10, (paddress.Y / ratio_y) - 10, 20, 20);

                e.Graphics.DrawEllipse(new Pen(Color.Blue, 2), (pother_address.X / ratio_x) - 10, (pother_address.Y / ratio_y) - 10, 20, 20);
            }



        }

        private void DrawPeer(PaintEventArgs e, Peer peer, bool semantic = false)
        {
            var ratio_x = Max / pictureBox1.Width;
            var ratio_y = Max / pictureBox1.Height;

            var address = peer.Address;

            if (semantic)
                address = peer.SemanticAddress;

            e.Graphics.FillEllipse(peer.Brush, (address.X / ratio_x) - 0, (address.Y / ratio_y) - 0, 2, 2);

            foreach (var packet in peer.Packets)
                e.Graphics.FillEllipse(peer.PacketBrush, (packet.X / ratio_x) - 0, (packet.Y / ratio_y) - 0, 2, 2);

            if (!onlyCircles && peer == selectedPeer)
                e.Graphics.DrawEllipse(SystemPens.ActiveBorder, (address.X / ratio_x) - 10, (address.Y / ratio_y) - 10, 1, 1);

            // if (!onlyCircles)
            foreach (var other_peer in peer.Peers)
            {
                var other_address = other_peer.Address;

                if (semantic)
                    other_address = other_peer.SemanticAddress;

                var p1 = new Point(address.X / ratio_x, address.Y / ratio_y);

                var p2 = new Point(other_address.X / ratio_x, other_address.Y / ratio_y);

                DrawBezier(peer, ratio_x, ratio_y, p1, p2, e);
            }

            if (!onlyCircles)
                e.Graphics.DrawString(peer.Id.ToString() + "\r\n" + (peer.AvgDistancia * 100).ToString("n2") + "\r\n" + peer.Peers.Count().ToString(), SystemFonts.CaptionFont, new SolidBrush(Color.White), (address.X / ratio_x) - 6, (address.Y / ratio_y));
        }

        private Point DrawBezier(Peer peer, int ratio_x, int ratio_y, Point p1, Point p2, PaintEventArgs e = null, bool justCalc = false, bool chain = false)
        {
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


            if (diff_x_straight > max_x / 2 || diff_y_straight > max_y / 2)
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
                    e.Graphics.DrawBezier(peer == selectedPeer ? SystemPens.ActiveBorder : (chain ? new Pen(Color.Red, 4) : peer.Pen), p1, p11, p22, p2);

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
            //  else
            {

                //e.Graphics.DrawLine(peer.Pen, p1.X, p1.Y, p2.X, p2.Y);

                var p111 = new Point(((p1.X + p2.X) / 2) + 20, ((p1.Y + p2.Y) / 2) + 20);

                var p222 = new Point(((p1.X + p2.X) / 2) - 20, ((p1.Y + p2.Y) / 2) - 20);

                if (!justCalc)
                    e.Graphics.DrawBezier(peer == selectedPeer ? SystemPens.ActiveBorder : (chain ? new Pen(Color.Red, 4) : peer.Pen), p1, p111, p222, p2);
            }

            return p2;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var ratio_x = Max / pictureBox1.Width;

            var ratio_y = Max / pictureBox1.Height;

            var point = this.PointToClient(MousePosition);

            var peer = new Peer(new Point(point.X * ratio_x, point.Y * ratio_y), Peers.Take(10));

            Peers.Add(peer);

            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            var ratio_x = Max / pictureBox1.Width;

            var ratio_y = Max / pictureBox1.Height;

            var mousep = this.PointToClient(MousePosition);

            var point = new Point(mousep.X * ratio_x, mousep.Y * ratio_y);

            var p1 = point;

            p1.Offset(-10, -10);

            var r = new Rectangle(p1, new Size(300, 300));

            var p = Peers.FirstOrDefault(x => r.Contains(x.Address));

            if (null != p)
            {
                selectedPeer = p;

                pictureBox1.Invalidate();

                pictureBox2.Invalidate();
            }


        }
    }
}
