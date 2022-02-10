using common.extends;
using System;
using System.Linq;

namespace common
{
    public static class StringHelper
    {
        public static string RandomPasswordString(int len)
        {
            string str = "1234567890abcdefghijklmnopqrstuvwxyz!@#$%^&*()_-+=<,>.?/:;\"'{[}].*";

            Random r = new Random();
            return string.Join("", str.OrderBy(x => r.Next(str.Length - 1)).Take(len));
        }

        public static string RandomPasswordStringMd5()
        {
            return string.Format("{0}{1}{2}", RandomPasswordString(32), Guid.NewGuid().ToString(), DateTimeHelper.GetTimeStamp()).Md5();
        }
    }
}
