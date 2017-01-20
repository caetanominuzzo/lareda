using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace library
{
    public class Cache<T> : List<CacheItem<T>>
    {
        public delegate void CacheExpiredHandler(CacheItem<T> item);

        public event CacheExpiredHandler OnCacheExpired;

        protected double Timeout;

        Timer activeTimeoutTimer;

        public Cache(double timeout = -1)
        {
            Timeout = timeout;

            activeTimeoutTimer = new Timer(
                activeTimeoutTimerCallback,
                null,
                -1,
                -1);
        }

        void activeTimeoutTimerCallback(object state)
        {
            activeTimeoutTimer.Change(-1, -1); 

            refresh();

            var any = false;

            lock(this)
                any = this.Any();

            if(any)
                activeTimeoutTimer.Change(pParameters.cacheActiveTimeoutInterval, pParameters.cacheActiveTimeoutInterval);
        }

        public CacheItem<T> Add(T value)
        {
            CacheItem<T> res;
            
            lock(this)
                res = this.FirstOrDefault(x => x.CachedValue.Equals(value));

            if (res != null)
            {
                res.Reset();
            }
            else
                lock (this)
                {
                    res = new CacheItem<T>(value);

                    base.Add(res);
                }

            activeTimeoutTimer.Change(pParameters.cacheActiveTimeoutInterval, pParameters.cacheActiveTimeoutInterval);

            return res;
        }

        IEnumerable<CacheItem<T>> refresh()
        {
            if(Timeout == 60)
            {

            }

            lock (this)
            {
                IEnumerable<CacheItem<T>> result = null;

                var now = DateTime.Now;

                if (OnCacheExpired != null)
                {
                    var expired = this.Where(x => (Timeout > 0 && now.Subtract(x.DateTime).TotalMilliseconds >= Timeout) ||
                                                  (Timeout == -1 && x.DateTime.Equals(DateTime.MinValue)));

                    expired.ToList().ForEach(x =>
                        {
                            this.Remove(x);

                            OnCacheExpired(x);

                            if(x is IDisposable)
                                ((IDisposable)x).Dispose();
                        });
                }
                else
                    this.RemoveAll(x => (Timeout > 0 && now.Subtract(x.DateTime).TotalMilliseconds >= Timeout) ||
                                        (Timeout == -1 && x.DateTime.Equals(DateTime.MinValue)));

                result = this.ToList();

                return result;
            }
        }

        internal void Reset()
        {
            lock (this)
                this.ForEach(x => x.Reset());
        }
    }

    public class CacheItem<T>
    {
        internal DateTime DateTime;

        public T CachedValue;

        public void Reset()
        {
            DateTime = System.DateTime.Now;
        }
        internal CacheItem(T value)
        {
            CachedValue = value;

            DateTime = DateTime.Now;
        }
    }

}

