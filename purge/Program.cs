using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace purge
{
    class Program
    {
        static List<byte[]> addresses = new List<byte[]>();

        static byte[] address;

        static void Main(string[] args)
        {
            address = library.Utils.GetAddress();

            for (var i = 0; i < 2000; i++)
                addresses.Add(library.Utils.GetAddress());

            while (true)
            {
                var c = Console.ReadKey().KeyChar;

                switch(c)
                {
                    case 'x': return;

                    case 'c': ThreadPool.QueueUserWorkItem(Create, null); break;

                    case 'p': ThreadPool.QueueUserWorkItem(Purge, null); break;

                    case 'r': ThreadPool.QueueUserWorkItem(Report, null); break;
                }
            }
        }

        static void Create(object o)
        {
            var k = 0;

            Report(o);

            for (var j = 0; j < 200000; j++)
            {
                for (var i = 0; i < 100; i++)
                    addresses.Add(library.Utils.GetAddress());

                Purge(o);

                if(k++ % 100 == 0)
                    Report(o);
            }
        }

        static void Purge(object o)
        {
            var d = new Dictionary<int, int>();

            lock(addresses)
            foreach (var a in addresses)
            {
                var dis = (int)library.Addresses.EuclideanDistance(address, a);

                if (d.ContainsKey(dis))
                    d[dis] = d[dis] + 1;
                else
                    d[dis] = 1;
            }

            //File.WriteAllLines("data2.txt", d.Select(x => x.Key.ToString() + ";" + x.Value.ToString()));

            var keys = d.Keys.OrderBy(x => x).ToArray();

            var sum_dist = 0;

            var sum_peers = 0;

            var limit = 20;

            var count = 0;

            foreach(var key in keys)
            {
                sum_peers += d[key];

                sum_dist += key * d[key];

                if (sum_peers > limit)
                    break;
            }



            var average = (double)sum_dist / sum_peers;

            average = average / 724.0773439;

            var toRemove = new List<byte[]>();

            var total = addresses.Count;

            var left = 2000;

            foreach (var a in addresses)
            {
                var pT = ((double)total - left) / total;
                
                //Probability by address distance to Local address
                var pL = 1-Math.Log(library.Addresses.EuclideanDistance(a, address)/ 724.0773439, average);



                if (pT > 0 && library.Utils.Roll((pT + pL*10)/11))
                {
                    toRemove.Add(a);

                    total--;

                    if (pL < 0)
                    {

                    }
                }
            }

           addresses.RemoveAll(x => toRemove.Any(y => library.Addresses.Equals(y,x)));

        }

        static void Report(object o)
        {
            var d = new Dictionary<int, int>();

            var i = 0;




            foreach (var a in addresses)
            {
                var dis = (int)library.Addresses.EuclideanDistance(address, a);

                if (d.ContainsKey(dis))
                    d[dis] = d[dis] + 1;
                else
                    d[dis] = 1;

               
            }

            var keys = d.Keys.OrderBy(x => x).ToArray();

            var sum_dist = 0;

            var sum_peers = 0;

            var limit = 20;

            var count = 0;

            foreach (var key in keys)
            {
                sum_peers += d[key];

                sum_dist += key * d[key];

                if (sum_peers > limit)
                    break;

                if (i++<10)Console.Write(key + ":" + d[key] + "\t");
            }



            var average = (double)sum_dist / sum_peers;

            average = average / 724.0773439;

            Console.WriteLine("\tavg: " + average.ToString("n4") +"\t" + addresses.Count);
        }


    }
}
