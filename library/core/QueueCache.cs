using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace library.core
{
    public class QueueCache<T>
    {
        protected Queue<CacheItem<T>> Data = new Queue<CacheItem<T>>();

        protected double Timeout;

        public QueueCache(double timeout)
        {
            Timeout = timeout;
        }

        public void Add(T value)
        {
             lock (Data)
            {
                Data.Enqueue(new CacheItem<T>(value));

                Refresh();
            }
        }

        protected void Refresh()
        {
            lock (Data)
            {
                while (!double.IsInfinity(Timeout) && Data.Any()
                        && (DateTime.Now.Subtract(Data.Peek().DateTime).TotalSeconds > Timeout))
                {
                    Data.Dequeue();
                }
            }
        }

        protected class CacheItem<T>
        {
            internal DateTime DateTime;

            internal T Value;

            internal CacheItem(T value)
            {
                Value = value;

                DateTime = DateTime.Now;
            }
        }
    }
}
