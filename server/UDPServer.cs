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

namespace server
{
    public class UDPServer : IUdpServer
    {
        public UDPServer()
        {
        }

        private UdpClient UdpcRecv { get; set; } = null;
        IPEndPoint IpepServer { get; set; } = null;
        private CancellationTokenSource cancellationTokenSource;

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
                IpepServer = new IPEndPoint(ip ?? IPAddress.Any, port);
                UdpcRecv = new UdpClient(IpepServer);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const uint IOC_IN = 0x80000000;
                    int IOC_VENDOR = 0x18000000;
                    int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                    UdpcRecv.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }

                _ = Task.Factory.StartNew((e) =>
                {
                    while (Running)
                    {
                        Receive();
                    }
                }, TaskCreationOptions.LongRunning, cancellationTokenSource.Token);
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
                IpepServer = null;
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

        private void Receive()
        {
            try
            {
                IPEndPoint ipepClient = null;
                byte[] bytRecv = UdpcRecv.Receive(ref ipepClient);
                OnPacketPushSub.Push(new ServerDataWrap<byte[]>
                {
                    Data = bytRecv,
                    Address = ipepClient,
                    ServerType = ServerType.UDP,
                    Socket = null
                });
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
            }
        }

        private SimplePushSubHandler<ServerDataWrap<byte[]>> onPacketPushSub = new SimplePushSubHandler<ServerDataWrap<byte[]>>();
        public SimplePushSubHandler<ServerDataWrap<byte[]>> OnPacketPushSub => onPacketPushSub;
    }
}
