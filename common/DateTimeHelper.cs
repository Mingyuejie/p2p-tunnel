using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public static class DateTimeHelper
    {
        private static DateTime time1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        public static long GetTimeStamp()
        {
            return Convert.ToInt64((DateTime.UtcNow - time1970).TotalMilliseconds);
        }
        public static long GetTimeStampSec()
        {
            return Convert.ToInt64((DateTime.UtcNow - time1970).TotalSeconds);
        }
    }
}
