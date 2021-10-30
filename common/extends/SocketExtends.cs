using common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace common.extends
{
    public static class SocketExtends
    {
        public static byte[] ReceiveAll(this NetworkStream stream)
        {
            List<byte> bytes = new();
            do
            {
                byte[] buffer = new byte[1024];
                int len = stream.Read(buffer);
                if (len == 0)
                {
                    return Array.Empty<byte>();
                }
                if (len < 1024)
                {
                    byte[] temp = new byte[len];
                    Array.Copy(buffer, 0, temp, 0, len);
                    buffer = temp;
                }
                bytes.AddRange(buffer);

            } while (stream.DataAvailable);

            return bytes.ToArray();


            //using MemoryStream ms = new MemoryStream();
            //do
            //{
            //    byte[] buffer = new byte[1024];
            //    int len = stream.Read(buffer);
            //    if (len == 0)
            //    {
            //        return Array.Empty<byte>();
            //    }
            //    ms.Write(buffer, 0, len);

            //} while (stream.DataAvailable);

            //return ms.ToArray();
        }
        public static void SafeClose(this Socket socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                }
                finally
                {
                    socket.Close();
                }
            }
        }
    }
}
