using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.extends
{
    public static class NumberExtends
    {
        public static byte[] GetBytes(this long num)
        {
            return BitConverter.GetBytes(num);
        }

        public static byte[] GetBytes(this ulong num)
        {
            return BitConverter.GetBytes(num);
        }

        public static byte[] GetBytes(this int num)
        {
            return BitConverter.GetBytes(num);
        }

        public static byte[] GetBytes(this short num)
        {
            return BitConverter.GetBytes(num);
        }
    }
}
