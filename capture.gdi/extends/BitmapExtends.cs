using System;
using System.Drawing;
using System.IO;

namespace common.extends
{
    public static class BitmapExtends
    {
        public static byte[] ToBytes(this Bitmap bmp)
        {
            using MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] data = new byte[ms.Length];
            ms.Seek(0, SeekOrigin.Begin);
            ms.Read(data, 0, Convert.ToInt32(ms.Length));
            return data;
        }
    }
}
