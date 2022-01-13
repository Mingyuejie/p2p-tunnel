using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public static class FileHelper
    {
        private static string[] sizeFormatStrings = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB" };
        public static string SizeFormat(long size)
        {
            float s = size;
            for (int i = 0; i < sizeFormatStrings.Length; i++)
            {
                if (s < 1024)
                {
                    return $"{s:0.##}{sizeFormatStrings[i]}";
                }
                s /= 1024;
            }
            return string.Empty;
        }
    }
}
