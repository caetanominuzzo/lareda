using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using library.core;

namespace library
{
    public class TimeCounter : QueueCache<double>
    {
        double Period;

        double _average;

        double _lastPeriod;

        public double Average
        {
            get
            {
                Refresh();

                return _average;
            }
        }

        public double TotalLastPeriod
        {
            get
            {
                Refresh();

                return _lastPeriod;
            }
        }

        public double TotalLastTimeout { get; private set; }

        public TimeCounter(double period, double timeout) : base(timeout)
        {
            Period = period;
        }

        new void Refresh()
        {
            base.Refresh();

            lock (Data)
            {
                TotalLastTimeout = Data.Sum(x => x.Value);

                _lastPeriod = Data.Where(x => DateTime.Now.Subtract(x.DateTime).TotalSeconds < Period)
                    .Sum(x => x.Value);


                if (Data.Any())
                {
                    if (double.IsInfinity(Timeout))
                        _average = TotalLastTimeout / Data.Count();
                    else
                        _average = (TotalLastTimeout / (Timeout / Period)) / Data.Count();
                }
            }
        }
    }
}
