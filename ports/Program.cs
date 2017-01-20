using library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ports
{
    class Program
    {
        static void Main(string[] args)
        {
            var s = Console.ReadLine();



            Console.WriteLine(Utils.Points(BitConverter.GetBytes((UInt16)int.Parse(s))));

            Console.ReadKey();
        }
    }
}
