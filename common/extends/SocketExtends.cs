using common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        public static void Reuse(this Socket socket,bool reuse = true)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuse);
        }
        public static void ReuseBind(this Socket socket,IPEndPoint ip)
        {
            socket.Reuse(true);
            socket.Bind(ip);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="time">多久没数据活动就发送一次</param>
        /// <param name="interval">间隔多久尝试一次</param>
        /// <param name="retryCount">尝试几次</param>
        public static void KeepAlive(this Socket socket, int time = 20, int interval = 5, int retryCount = 5)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, interval);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, retryCount);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, time);
            //if (socket != null)
            //{
            //    try
            //    {
            //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //        {
            //            socket.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);
            //        }
            //    }
            //    catch (Exception)
            //    {
            //    }
            //}
        }
        private static byte[] keepaliveData = null;
        public static byte[] GetKeepAliveData()
        {
            if (keepaliveData == null)
            {
                uint dummy = 0;
                byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));//keep-alive间隔
                BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);// 尝试间隔
                keepaliveData = inOptionValues;
            }
            return keepaliveData;
        }
    }
}
