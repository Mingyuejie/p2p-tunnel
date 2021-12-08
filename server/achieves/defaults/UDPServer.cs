using common;
using server.model;
using server.packet;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace server.achieves.defaults
{
    public class UDPServer : IUdpServer
    {
        public UDPServer()
        {
        }

        private UdpClient UdpcRecv { get; set; } = null;
        private CancellationTokenSource cancellationTokenSource;
        public SimplePushSubHandler<ServerDataWrap<UdpPacket>> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap<UdpPacket>>();

        private bool Running
        {
            get
            {
                return cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested;
            }
        }

        public void Start(int port, IPAddress ip = null)
        {
            if (!Running)
            {
                cancellationTokenSource = new CancellationTokenSource();
                UdpcRecv = new UdpClient(new IPEndPoint(ip ?? IPAddress.Any, port));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const uint IOC_IN = 0x80000000;
                    int IOC_VENDOR = 0x18000000;
                    int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                    UdpcRecv.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }

                IAsyncResult res = UdpcRecv.BeginReceive(Receive, null);
            }
        }

        private void Receive(IAsyncResult result)
        {
            try
            {
                IPEndPoint ipepClient = null;
                byte[] bytRecv = UdpcRecv.EndReceive(result, ref ipepClient);
                result.AsyncWaitHandle.Close();

                UdpPacket packet = UdpPacket.FromArray(ipepClient, bytRecv);
                if (packet != null)
                {
                    OnPacket.Push(new ServerDataWrap<UdpPacket>
                    {
                        Data = packet,
                        Address = ipepClient,
                        ServerType = ServerType.UDP,
                        Socket = null
                    });
                }
                IAsyncResult res = UdpcRecv.BeginReceive(Receive, null);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            if (UdpcRecv != null)
            {
                UdpcRecv.Close();
                UdpcRecv.Dispose();
                UdpcRecv = null;
            }
        }

        public bool Send(byte[] data, IPEndPoint address)
        {
            if (UdpcRecv != null && address != null)
            {
                try
                {
                    _ = UdpcRecv.SendAsync(data, data.Length, address);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex + "");
                }
            }
            return false;
        }
    }
}
