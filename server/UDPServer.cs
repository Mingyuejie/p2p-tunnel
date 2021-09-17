using common;
using common.extends;
using server.extends;
using server.model;
using server.models;
using server.packet;
using server.plugin;
using System;
using System.Collections.Generic;
using System.Linq;
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
        long sequence = 0;
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

        public async Task<ServerMessageResponeWrap> SendReply<T>(SendMessageWrap<T> msg)
        {
            var tcs = new TaskCompletionSource<ServerMessageResponeWrap>();
            if (UdpcRecv != null && msg.Address != null)
            {
                try
                {
                    if (msg.RequestId == 0)
                    {
                        Interlocked.Increment(ref Plugin.requestId);
                        msg.RequestId = Plugin.requestId;
                    }

                    Plugin.sends.TryAdd(msg.RequestId, new SendCacheModel { Tcs = tcs, RequestId = msg.RequestId });

                    ServerMessageWrap wrap = new ServerMessageWrap
                    {
                        RequestId = msg.RequestId,
                        Content = msg.Data.ToBytes(),
                        Path = msg.Path,
                        Code = msg.Code,
                        Type = msg.Type
                    };

                    _ = Interlocked.Increment(ref sequence);
                    IEnumerable<UdpPacket> udpPackets = wrap.ToUdpPackets(sequence);

                    foreach (UdpPacket udpPacket in udpPackets)
                    {
                        byte[] udpPacketDatagram = udpPacket.ToArray();
                        _ = UdpcRecv.SendAsync(udpPacketDatagram, udpPacketDatagram.Length, msg.Address);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex + "");
                }
            }
            else
            {
                tcs.SetResult(new ServerMessageResponeWrap { Code = ServerMessageResponeCodes.BAD_GATEWAY, ErrorMsg = "未运行" });
            }
            return await tcs.Task;
        }

        public void SendOnly<T>(SendMessageWrap<T> msg)
        {
            if (UdpcRecv != null && msg.Address != null)
            {
                try
                {
                    if (msg.RequestId == 0)
                    {
                        Interlocked.Increment(ref Plugin.requestId);
                        msg.RequestId = Plugin.requestId;
                    }
                    ServerMessageWrap wrap = new ServerMessageWrap
                    {
                        RequestId = msg.RequestId,
                        Content = msg.Data.ToBytes(),
                        Path = msg.Path,
                        Code = msg.Code,
                        Type = msg.Type
                    };

                    _ = Interlocked.Increment(ref sequence);
                    IEnumerable<UdpPacket> udpPackets = wrap.ToUdpPackets(sequence);

                    foreach (UdpPacket udpPacket in udpPackets)
                    {
                        byte[] udpPacketDatagram = udpPacket.ToArray();
                        _ = UdpcRecv.SendAsync(udpPacketDatagram, udpPacketDatagram.Length, msg.Address);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex + "");
                }
            }
        }

        private void Receive()
        {
            try
            {
                IPEndPoint ipepClient = null;
                byte[] bytRecv = UdpcRecv.Receive(ref ipepClient);

                UdpPacket packet = UdpPacket.FromArray(ipepClient, bytRecv);
                if (packet != null)
                {

                    ServerMessageWrap wrap = packet.Chunk.DeBytes<ServerMessageWrap>();
                    if (wrap.Type == ServerMessageTypes.RESPONSE)
                    {
                        if (Plugin.sends.TryRemove(wrap.RequestId, out SendCacheModel send) && send != null)
                        {
                            send.Tcs.SetResult(new ServerMessageResponeWrap { Code = wrap.Code, ErrorMsg = wrap.Code.ToString(), Data = wrap.Content });
                        }
                    }
                    else
                    {
                        try
                        {
                            wrap.Path = wrap.Path.ToLower();
                            if (Plugin.plugins.ContainsKey(wrap.Path))
                            {
                                var plugin = Plugin.plugins[wrap.Path];
                                PluginParamWrap excute = new PluginParamWrap
                                {
                                    SourcePoint = ipepClient,
                                    Packet = packet,
                                    ServerType = ServerType.UDP,
                                    Wrap = wrap
                                };
                                object resultAsync = plugin.Item2.Invoke(plugin.Item1, new object[] { excute });
                                if (excute.Code == ServerMessageResponeCodes.OK)
                                {
                                    object resultObject = null;
                                    if (resultAsync is Task task)
                                    {
                                        task.Wait();
                                        if (resultAsync is Task<object> task1)
                                        {
                                            resultObject = task1.Result;
                                        }
                                    }
                                    else
                                    {
                                        resultObject = resultAsync;
                                    }
                                    if (resultObject != null)
                                    {
                                        SendOnly(new SendMessageWrap<object>
                                        {
                                            Address = ipepClient,
                                            Type = ServerMessageTypes.RESPONSE,
                                            Data = resultObject ?? new EmptyModel(),
                                            RequestId = wrap.RequestId,
                                            Path = wrap.Path
                                        });
                                    }
                                }
                                else
                                {
                                    SendOnly(new SendMessageWrap<object>
                                    {
                                        Address = ipepClient,
                                        Type = ServerMessageTypes.RESPONSE,
                                        Data = excute.ErrorMessage,
                                        RequestId = wrap.RequestId,
                                        Path = wrap.Path
                                    });
                                }
                            }
                            else
                            {
                                SendOnly(new SendMessageWrap<object>
                                {
                                    Address = ipepClient,
                                    Type = ServerMessageTypes.RESPONSE,
                                    Code = ServerMessageResponeCodes.BAD_GATEWAY,
                                    Data = "没找到对应的插件执行你的操作",
                                    RequestId = wrap.RequestId,
                                    Path = wrap.Path
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            SendOnly(new SendMessageWrap<object>
                            {
                                Address = ipepClient,
                                Type = ServerMessageTypes.RESPONSE,
                                Code = ServerMessageResponeCodes.BAD_GATEWAY,
                                Data = ex.Message,
                                RequestId = wrap.RequestId,
                                Path = wrap.Path
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
            }
        }
    }
}
