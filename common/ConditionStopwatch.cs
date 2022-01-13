using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class ConditionStopwatch : Stopwatch
    {
        [Conditional("DEBUG")]
        public new void Start()
        {
            base.Start();
        }

        [Conditional("DEBUG")]
        public new void Reset()
        {
            base.Reset();
        }

        [Conditional("DEBUG")]
        public new void Stop()
        {
            base.Stop();
        }

        [Conditional("DEBUG")]
        public void Output(string remark)
        {
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"=============\n{remark}:{ElapsedMilliseconds} ms、{ElapsedTicks} ticks、{GetUs():n3} us\n============");
            Console.ForegroundColor = currentForeColor;
        }

        public long GetUs()
        {
            return (ElapsedTicks * 1000000 / Frequency);
        }
    }
}
