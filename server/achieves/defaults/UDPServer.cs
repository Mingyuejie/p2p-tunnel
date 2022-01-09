using common;
using common.extends;
using server.model;
using server.packet;
using System;
using System.Collections.Concurrent;
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
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public SimplePushSubHandler<ServerDataWrap<UdpPacket>> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap<UdpPacket>>();
        private static ConcurrentDictionary<long, IConnection> clients = new ConcurrentDictionary<long, IConnection>();

        public void Start(int port, IPAddress ip = null)
        {
            if (UdpcRecv != null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            UdpcRecv = new UdpClient(new IPEndPoint(ip ?? IPAddress.Any, port));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const uint IOC_IN = 0x80000000;
                int IOC_VENDOR = 0x18000000;
                int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                UdpcRecv.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var result = await UdpcRecv.ReceiveAsync();
                        long id = result.RemoteEndPoint.ToInt64();
                        if (!clients.TryGetValue(id, out IConnection connection))
                        {
                            connection = CreateConnection(result.RemoteEndPoint);
                            clients.TryAdd(id, connection);
                        }
                        UdpPacket packet = UdpPacket.FromArray(id, result.Buffer);
                        if (packet != null)
                        {
                            await OnPacket.PushAsync(new ServerDataWrap<UdpPacket>
                            {
                                Data = packet,
                                Connection = connection
                            });
                        }
                    }
                    catch (Exception)
                    {
                        Stop();
                        break;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        Stop();
                        break;
                    }
                }
            }, cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
            if (UdpcRecv != null)
            {
                UdpcRecv.Close();
                UdpcRecv.Dispose();
                UdpcRecv = null;
            }

            foreach (IConnection item in clients.Values)
            {
                item.Disponse();
            }
            clients.Clear();
        }

        public IConnection CreateConnection(IPEndPoint address)
        {
            return new Connection
            {
                ServerType = ServerType.UDP,
                UdpAddress = address,
                UdpAddress64 = address.ToInt64(),
                UdpcRecv = UdpcRecv
            };
        }
    }
}
