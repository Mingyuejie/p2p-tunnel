using System.Drawing;
using System.IO;

namespace common.extends
{
    public static class ByteArrayExtends
    {
        public static Bitmap ToBitmap(this byte[] bytes)
        {
            MemoryStream ms = new MemoryStream(bytes);
            Bitmap temp = new Bitmap(ms);
            Bitmap bmp = new Bitmap(temp);
            ms.Close();
            ms.Dispose();
            return bmp;
        }
    }
}
