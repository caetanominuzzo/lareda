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

            public List<Peer> Peers = new List<Peer>();

            public List<Point> Packets = new List<Point>();

            public Color PeerColor = Color.FromArgb(255, rand.Next(256), rand.Next(256), rand.Next(256));

            public Brush Brush;

            public Brush PacketBrush;

            public Pen Pen;

            public int Id;

            public double AvgDistancia = 0;

            public Peer(Point address, IEnumerable<Peer> parent = null)
            {
                Address = address;

                PeerColor = Color.FromArgb(255, Form1.colors.GetPixel(Address.X * 100 / Max, Address.Y * 100 / Max));

                Brush = new SolidBrush(PeerColor);

                var pColor = Color.FromArgb(255, PeerColor);

                PacketBrush = new SolidBrush(pColor);

                Pen = new Pen(pColor, 1);

                if (null != parent)
                {
                    foreach (var p in parent)
                    {
                        Peers.Add(p);

                        p.Peers.Add(this);
                    }
                }

                Id = Form1.Id++;
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

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'o')
            {
                onlyCircles = !onlyCircles;
            }

            if (e.KeyChar == 'a')
            {
                var peer = new Peer(Rand(), Peers.Take(10));

                Peers.Add(peer);

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
                        var closests_from_closest = closest.Peers.OrderBy(x => rand.Next()).Take(4);

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

                    var perc_to_remove = 0.1;

                    var total = peer.Peers.Count();

                    var total_to_remove = total * perc_to_remove;

                    var left = total - total_to_remove;

                    if (total_to_remove < 1)
                        continue;

                    var toRemove = new List<Peer>();

                    foreach (var other_peer in peer.Peers.OrderBy(x => rand.Next()))
                    {
                        var pT = ((double)total - left) / total;
                        
                        var pL = Math.Log(100 * (Distance(peer.Address, other_peer.Address) / Max), (perc_avg*100)) - 1;

                        if (pT > 0 && library.Utils.Roll((pT + pL * 1) / 2))
                        {
                            toRemove.Add(other_peer);

                            total--;
                        }

                    }

                    peer.Peers.RemoveAll(x => toRemove.Any(y => y == x));
                }

            }

            pictureBox1.Invalidate();


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

            foreach (var peer in Peers)
            {
                DrawPeer(e, peer);
            }

            if (null != selectedPeer)
                DrawPeer(e, selectedPeer);

        }

        private void DrawPeer(PaintEventArgs e, Peer peer)
        {
            var ratio_x = Max / pictureBox1.Width;
            var ratio_y = Max / pictureBox1.Height;

            e.Graphics.FillEllipse(peer.Brush, (peer.Address.X / ratio_x) - 0, (peer.Address.Y / ratio_y) - 0, 2, 2);

            foreach (var packet in peer.Packets)
                e.Graphics.FillEllipse(peer.PacketBrush, (packet.X / ratio_x) - 0, (packet.Y / ratio_y) - 0, 2, 2);

            if (!onlyCircles && peer == selectedPeer)
                e.Graphics.DrawEllipse(SystemPens.ActiveBorder, (peer.Address.X / ratio_x) - 10, (peer.Address.Y / ratio_y) - 10, 1, 1);

            if (!onlyCircles)
                foreach (var other_peer in peer.Peers)
                {
                    var p1 = new Point(peer.Address.X / ratio_x, peer.Address.Y / ratio_y);

                    var p2 = new Point(other_peer.Address.X / ratio_x, other_peer.Address.Y / ratio_y);

                    var diff_x_straight = Math.Abs(p2.X - p1.X);

                    var diff_y_straight = Math.Abs(p2.Y - p1.Y);

                    var diff_x_counter = p1.X - p2.X;

                    var diff_y_counter = p1.Y - p2.Y;

                    var max_x = Max / ratio_x;

                    var max_y = Max / ratio_y;

                    if (diff_x_straight > max_x / 2 || diff_y_straight > max_y / 2)
                    {
                        if (diff_x_straight > max_x / 2)
                        {
                            if (diff_x_counter < 0)
                                p1.X = p1.X + max_x;
                            else
                                p1.X = p1.X - max_x;
                        }

                        if (diff_y_straight > max_y / 2)
                        {
                            if (diff_y_counter < 0)
                                p1.Y = p1.Y + max_y;
                            else
                                p1.Y = p1.Y - max_y;
                        }

                        //e.Graphics.DrawLine(peer.Pen, p1.X, p1.Y, p2.X, p2.Y);

                        var p11 = new Point(((p1.X + p2.X) / 2) + 20, ((p1.Y + p2.Y) / 2) + 20);

                        var p22 = new Point(((p1.X + p2.X) / 2) - 20, ((p1.Y + p2.Y) / 2) - 20);

                        //e.Graphics.DrawBezier(peer == selectedPeer ? SystemPens.ActiveBorder : peer.Pen, p1, p11, p22, p2);

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



                    }

                    //e.Graphics.DrawLine(peer.Pen, p1.X, p1.Y, p2.X, p2.Y);

                    var p111 = new Point(((p1.X + p2.X) / 2) + 20, ((p1.Y + p2.Y) / 2) + 20);

                    var p222 = new Point(((p1.X + p2.X) / 2) - 20, ((p1.Y + p2.Y) / 2) - 20);

                    //e.Graphics.DrawBezier(peer == selectedPeer ? SystemPens.ActiveBorder : peer.Pen, p1, p111, p222, p2);
                }

            if(!onlyCircles)
                e.Graphics.DrawString(peer.Id.ToString() + "\r\n" + (peer.AvgDistancia * 100).ToString("n2") + "\r\n" + peer.Peers.Count().ToString(), SystemFonts.CaptionFont, new SolidBrush(Color.White), (peer.Address.X / ratio_x) - 6, (peer.Address.Y / ratio_y));
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
            }


        }
    }
}
