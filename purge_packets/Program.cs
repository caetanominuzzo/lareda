using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace purge_packets
{
    //class Point
    //{
    //    internal int X;
    //    internal int Y;

    //    internal Point(int x, int y)
    //    {
    //        X = x;

    //        Y = y;
    //    }


}

class Program
{
    internal static int Max = 10120;

    static Random rand = new Random();

    internal static Point Rand()
    {
        return new Point(rand.Next(Max), rand.Next(Max));
    }

    static Dictionary<Point, Point> packets = new Dictionary<Point, Point>();

    static List<Point> peers = new List<Point>();

    static void Main(string[] args)
    {
        peers.Add(new Point(Max / 4, Max / 4));

        peers.Add(new Point(Max * 3 / 4, Max *3/4 ));

        foreach (var peer in peers)
        {
            for (var i = 0; i < 2000; i++)
                packets.Add(Rand(), peer);
        }

        for (var i = 0; i < 100; i++)
            packets.Add(Rand(), peers[0]);

        Print(null);


        while (true)
        {
            var c = Console.ReadKey().KeyChar;

            switch (c)
            {
                case 'x': return;

                case 'c': ThreadPool.QueueUserWorkItem(Create, null); break;

                case 'p': ThreadPool.QueueUserWorkItem(Sync, null); break;

                case 'r': ThreadPool.QueueUserWorkItem(Report, null); break;
            }
        }
    }


    static void Print(object o)
    {
        var colors = new Color[] { Color.Blue, Color.Red, Color.Green, Color.Yellow };

        var pens = colors.Select(x => new Pen(x, 1)).ToArray();

        var brushes = colors.Select(x => new SolidBrush(x)).ToArray();

        var ratio = 3;

        var max = (int)(Max / ratio);

        Bitmap bmp = new Bitmap(max - 1, max - 1);

        Rectangle ImageSize = new Rectangle(0, 0, max - 1, max - 1);

        using (Graphics graph = Graphics.FromImage(bmp))
        {
            graph.FillRectangle(Brushes.White, ImageSize);

            foreach (var p in packets)
            {
                var index = peers.IndexOf(p.Value);

                graph.FillEllipse(brushes[index], (p.Key.X-50)/ ratio, (p.Key.Y-50)/ ratio, 100, 100);

                //bmp.SetPixel(p.Key.X, p.Key.Y, Color.Red);
            }

            var file = DateTime.Now.ToString().Replace("/", "_").Replace(":", "_");

            if (null != o)
                file = "d" + ((int)o).ToString().PadLeft(4,'0');

            bmp.Save(file + ".png", ImageFormat.Png);
        }
    }

    static void Create(object o)
    {
        //var k = 0;

        //Report(o);

        //for (var j = 0; j < 200000; j++)
        //{
        //    for (var i = 0; i < 100; i++)
        //        packets.Add(library.Utils.GetAddress());

        //    Purge(o);

        //    if (k++ % 100 == 0)
        //        Report(o);
        //}
    }

    public static double EuclideanDistance(int[] x, int[] y)
    {
        double sum = 0;

        for (var i = 0; i < x.Length; i++)
        {
            double diff = Math.Abs(x[i] - y[i]);

            sum += Math.Pow(Math.Min(diff, Max - diff), 2);
        }

        sum = Math.Sqrt(sum);

        return sum;
    }

    static void Sync(object o)
    {
        for (var z = 0; z < 1000; z++)
        {
            if (z == 200)
                peers.Add(new Point(Max / 2, Max / 2));

            if (z == 400)
                peers.Add(new Point(Max *3 / 4, Max / 4));

            var peers_count = 0;

            var peers_dist_sum = 0;

            foreach (var peer in peers)
            {
                foreach (var other_peer in peers)
                {
                    if (peer == other_peer)
                        continue;

                    peers_count++;

                    peers_dist_sum += (int)EuclideanDistance(new int[] { peer.X, peer.Y }, new int[] { other_peer.X, other_peer.Y });
                }

                var avg_peer_dist = (double)peers_dist_sum / peers_count;


                var perc_avg = avg_peer_dist / Max;



                var left = 250;

                var packs = packets.Where(x => x.Value == peer).ToArray();

                var total = packs.Count();

                foreach (var a in packs)
                {
                    var pT = ((double)total - left) / total;

                    //Probability by address distance to Local address
                    var pL = 1 - Math.Log(EuclideanDistance(new int[] { peer.X, peer.Y }, new int[] { a.Key.X, a.Key.Y }) / Max, perc_avg);

                    if (pL > 1)
                    {
                    }

                    if (pT > 0 && library.Utils.Roll((pT + pL * 1) / 2))
                    {
                        Point closer_peer = peer;

                        foreach (var other_peer in peers)
                        {
                            if (peer == other_peer)
                                continue;

                            if (closer_peer == peer || EuclideanDistance(new int[] { a.Key.X, a.Key.Y }, new int[] { other_peer.X, other_peer.Y }) < EuclideanDistance(new int[] { a.Key.X, a.Key.Y }, new int[] { closer_peer.X, closer_peer.Y }))
                                closer_peer = other_peer;
                        }

                        packets[a.Key] = closer_peer;

                        total--;
                    }
                }

                
            }
            Print(z);
        }
    }

    static void Report(object o)
    {
        //var d = new Dictionary<int, int>();

        //var i = 0;




        //foreach (var a in packets)
        //{
        //    var dis = (int)library.Addresses.EuclideanDistance(peers, a);

        //    if (d.ContainsKey(dis))
        //        d[dis] = d[dis] + 1;
        //    else
        //        d[dis] = 1;


        //}

        //var keys = d.Keys.OrderBy(x => x).ToArray();

        //var sum_dist = 0;

        //var sum_peers = 0;

        //var limit = 20;

        //var count = 0;

        //foreach (var key in keys)
        //{
        //    sum_peers += d[key];

        //    sum_dist += key * d[key];

        //    if (sum_peers > limit)
        //        break;

        //    if (i++ < 10) Console.Write(key + ":" + d[key] + "\t");
        //}



        //var average = (double)sum_dist / sum_peers;

        //average = average / 724.0773439;

        //Console.WriteLine("\tavg: " + average.ToString("n4") + "\t" + packets.Count);
    }


}

