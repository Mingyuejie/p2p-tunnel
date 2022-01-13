using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace common
{
    public static class TimerIntervalHelper
    {
        private static readonly ConcurrentDictionary<ulong, Timer> setTimeoutCache = new();
        private static NumberSpace setTimeoutNs = new NumberSpace(0);
        public static ulong SetTimeout(Action action, double interval)
        {
            ulong id = setTimeoutNs.Get();

            Timer t = new(interval);
            t.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs e) =>
            {
                action();
                Close(id);

            });
            t.AutoReset = false;
            t.Enabled = true;
            t.Start();

            setTimeoutCache.TryAdd(id, t);

            return id;

        }
        public static ulong SetInterval(Action action, double interval)
        {
            ulong id = setTimeoutNs.Get();

            Timer t = new(interval);
            t.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs e) =>
            {
                action();
            });
            t.AutoReset = true;
            t.Enabled = true;
            t.Start();

            setTimeoutCache.TryAdd(id, t);
            return id;
        }

        public static void Close(ulong id)
        {
            if (setTimeoutCache.TryRemove(id, out System.Timers.Timer t))
            {
                t.Close();
            }
        }
    }
}
