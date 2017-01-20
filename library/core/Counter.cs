using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library
{
    public class Counter
    {
        protected Queue<double> Data = new Queue<double>();

        protected double Size;

        public Counter(double size)
        {
            Size = size;
        }

        double _average = 0;

        int _currentSize = 0;

        public double Average
        {
            get
            {
                return _average;
            }
        }

        public void Add(double value)
        {
            lock (Data)
            {
                Data.Enqueue(value);

                _average += value / Data.Count();
            }
        }

    }
}
