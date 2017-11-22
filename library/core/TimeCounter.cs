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

        DateTime last_refresh = DateTime.MinValue;

        new void Refresh()
        {
            var now = DateTime.Now;

            if (now.Subtract(last_refresh).TotalMilliseconds < 20)
                return;

            last_refresh = now;

            base.Refresh();

            lock (Data)
            {
                TotalLastTimeout = Data.Sum(x => x.Value);

                _lastPeriod = Data.Where(x => now.Subtract(x.DateTime).TotalSeconds < Period)
                    .Sum(x => x.Value);

                if (Data.Any())
                {
                    var lastAvg = TotalLastTimeout / Data.Count();

                    if (double.IsInfinity(Timeout))
                        _average = TotalLastTimeout / Data.Count();
                    else
                        _average = _average + (lastAvg - _average) / ((Timeout / Period) + 1);// (TotalLastTimeout / (Timeout / Period)) / Data.Count();
                }
            }
        }
    }
}
