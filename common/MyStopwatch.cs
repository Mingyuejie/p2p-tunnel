using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    /// <summary>
    /// 计时器，只在DEBUG下有效
    /// </summary>
    public class MyStopwatch : Stopwatch
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
            Console.WriteLine($"=============\n{remark}:{base.ElapsedMilliseconds}ms {base.ElapsedTicks}ticks\n============");
            Console.ForegroundColor = currentForeColor;
        }
    }
}
