﻿using common;
using common.extends;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace server.servers.defaults
{
    public class UDPServer : IUdpServer
    {
        public UDPServer()
        {
        }

        private UdpClient UdpcRecv { get; set; } = null;
        public SimpleSubPushHandler<IConnection> OnPacket { get; } = new SimpleSubPushHandler<IConnection>();
        private static ConcurrentDictionary<long, IConnection> clients = new ConcurrentDictionary<long, IConnection>();
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();


        public void Start(int port, IPAddress ip = null)
        {
            if (UdpcRecv != null)
            {
                return;
            }

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
                        var result = await UdpcRecv.ReceiveAsync(cancellationToken.Token);
                        long id = result.RemoteEndPoint.ToInt64();
                        if (!clients.TryGetValue(id, out IConnection connection))
                        {
                            connection = CreateConnection(result.RemoteEndPoint);
                            clients.TryAdd(id, connection);
                        }

                        if (result.Buffer.Length > 0)
                        {
                            connection.ReceiveDataWrap.Data = result.Buffer.AsMemory();
                            await OnPacket.PushAsync(connection);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError(ex);
                        Stop();
                        break;
                    }

                    if (UdpcRecv == null)
                    {
                        Stop();
                        break;
                    }
                }
            });
        }

        public void Stop()
        {
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
            return new UdpConnection(UdpcRecv, address);
        }
    }
}
