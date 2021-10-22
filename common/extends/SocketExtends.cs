using common;
using System;
using System.Collections.Generic;
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



            //byte[] first = new byte[1024];
            //int len = stream.Read(first);
            //if(len == 0)
            //{
            //    return Array.Empty<byte>();
            //}
            //if (len < first.Length)
            //{
            //    Console.WriteLine($"first:{len}");
            //    return first.AsSpan().Slice(0, len).ToArray();
            //}
            //if (stream.DataAvailable)
            //{
            //    Console.WriteLine($"DataAvailable:{stream.Length}");
            //    byte[] buffer = new byte[stream.Length];
            //    int len1 = stream.Read(buffer);

            //    byte[] res = new byte[len + len1];
            //    Array.Copy(first, 0, res, 0, len);
            //    Array.Copy(buffer, 0, res, len, len1);

            //    return res;
            //}
            //else
            //{
            //    return first;
            //}
        }


        public static byte[] ReceiveAll(this Socket socket)
        {
            byte[] first = new byte[1024];
            int len = socket.Receive(first);
            if (len == 0)
            {
                return Array.Empty<byte>();
            }
            if (len < first.Length)
            {
                return first.AsSpan().Slice(0, len).ToArray();
            }
            if (socket.Available > 0)
            {
                byte[] buffer = new byte[socket.Available];
                int len1 = socket.Receive(buffer);

                byte[] res = new byte[len + len1];
                Array.Copy(first, 0, res, 0, len);
                Array.Copy(buffer, 0, res, len, len1);
                return res;
            }
            else
            {
                return first;
            }
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
