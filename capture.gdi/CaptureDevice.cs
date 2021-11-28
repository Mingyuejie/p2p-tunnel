using capture.gdi.native;
using capture.gdi.native.gdi32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace capture.gdi
{
    public sealed class CaptureDevice
    {
        private readonly IntPtr windowHandle;

        private IntPtr bitmapHandle;

        private IntPtr destCtx;

        private IntPtr drawCtx;

        private readonly int x;
        private readonly int y;
        private readonly int width;
        private readonly int height;

        public CaptureDevice(int x, int y, int width, int height)
        {
            this.windowHandle = User32.GetDesktopWindow();
            this.drawCtx = User32.GetWindowDC(this.windowHandle);
            this.destCtx = Gdi32.CreateCompatibleDC(this.drawCtx);
            this.bitmapHandle = Gdi32.CreateCompatibleBitmap(this.drawCtx, width, height);

            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public void AcquireFrame()
        {
            Gdi32.SelectObject(this.destCtx, this.bitmapHandle);
            Gdi32.BitBlt(this.destCtx,
                         0,
                         0,
                         width,
                         height,
                         this.drawCtx,
                         x,
                         y,
                         Gdi32.TernaryRasterOperations.SRCCOPY);
        }

        public void ReleaseFrame()
        {
            if (this.drawCtx != IntPtr.Zero)
            {
                User32.ReleaseDC(this.windowHandle, this.drawCtx);
            }

            if (this.destCtx != IntPtr.Zero)
            {
                User32.ReleaseDC(this.windowHandle, this.destCtx);
            }

            if (this.bitmapHandle != IntPtr.Zero)
            {
                Gdi32.DeleteObject(this.bitmapHandle);
            }

            this.drawCtx = this.destCtx = this.bitmapHandle = IntPtr.Zero;
        }

        public GdiBitmapFrame LockFrame()
        {
            Bitmap bmp = Image.FromHbitmap(this.bitmapHandle);
            BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size),
                                           ImageLockMode.ReadWrite,
                                           PixelFormat.Format32bppArgb);
            return new GdiBitmapFrame(bmp, data);
        }

        public void UnlockFrame(GdiBitmapFrame frame)
        {
            if (frame is GdiBitmapFrame gdiVideoFrame)
            {
                gdiVideoFrame.Bitmap.UnlockBits(gdiVideoFrame.BitmapData);
                gdiVideoFrame.Bitmap.Dispose();
            }
        }
    }

    public class GdiBitmapFrame
    {
        public Bitmap Bitmap { get; }

        public BitmapData BitmapData { get; }

        public int Width { get; }

        public int Height { get; }

        internal GdiBitmapFrame(Bitmap bitmap, BitmapData bitmapLock)
        {
            Width = bitmap.Size.Width;
            Height = bitmap.Size.Height;

            Bitmap = bitmap;
            BitmapData = bitmapLock;
        }
    }
}
