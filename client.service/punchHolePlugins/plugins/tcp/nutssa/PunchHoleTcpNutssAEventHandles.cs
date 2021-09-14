using client.service.events;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.register;
using common;
using common.extends;
using PacketDotNet;
using ProtoBuf;
using server;
using server.model;
using server.models;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.punchHolePlugins.plugins.tcp.nutssa
{
    public class PunchHoleTcpNutssAEventHandles : IPunchHoleTcp
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly EventHandlers eventHandlers;
        private readonly ITcpServer tcpServer;
        private readonly RegisterState registerState;

        public PunchHoleTcpNutssAEventHandles(EventHandlers eventHandlers, PunchHoleEventHandles punchHoldEventHandles, ITcpServer tcpServer, RegisterState registerState)
        {
            this.eventHandlers = eventHandlers;
            this.punchHoldEventHandles = punchHoldEventHandles;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
        }

        private Socket TcpServer => registerState.TcpSocket;
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        public int ClientTcpPort => registerState.RemoteInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<long, ConnectTcpCache> connectTcpCache = new();

        #region 连接客户端


        /// <summary>
        /// 发送TCP连接客户端请求消息（给服务器）
        /// </summary>
        public event EventHandler<SendEventArg> OnSendTcpHandler;
        /// <summary>
        /// 发送TCP连接客户端请求消息（给服务器）
        /// </summary>
        /// <param name="toid"></param>
        public void Send(ConnectTcpParams param)
        {
            SendStepPacket(new SendStepPacketEventArg { FromId = ConnectId, ToId = param.Id, Socket = TcpServer });
            SendStepPacket(new SendStepPacketEventArg { FromId = param.Id, ToId = ConnectId, Socket = TcpServer });
            // SendPrivate(param);
        }

        public void SendPrivate(ConnectTcpParams param)
        {
            Logger.Instance.Debug($"SendPrivate {param.Id}->{param.Name}");
            OnSendTcpHandler?.Invoke(this, new SendEventArg
            {
                Id = param.Id,
                Name = param.Name
            });

            connectTcpCache.TryAdd(param.Id, new ConnectTcpCache
            {
                Callback = param.Callback,
                FailCallback = param.FailCallback,
                Time = Helper.GetTimeStamp(),
                Timeout = param.Timeout
            });
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = param.Id,
                Data = new Step1Model
                {
                    FromId = ConnectId,
                    PunchType = PunchHoleTypes.TCP_NUTSSA
                }
            });

            long id = param.Id;
            Helper.SetTimeout(() =>
            {
                if (connectTcpCache.TryGetValue(id, out ConnectTcpCache cache))
                {
                    connectTcpCache.TryRemove(id, out _);
                    cache?.FailCallback(new ConnectFailModel
                    {
                        Msg = "TCP连接超时",
                        Type = ConnectFailType.TIMEOUT
                    });
                }
            }, param.Timeout);
        }

        public event EventHandler<OnStep1EventArg> OnStep1Handler;
        public void OnStep1(OnStep1EventArg e)
        {
            Logger.Instance.Debug($"OnStep1 {e.Data.Id}");
            OnStep1Handler?.Invoke(this, e);

            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = e.Data.Id,
                Data = new Step2Model
                {
                    FromId = ConnectId,
                    PunchType = PunchHoleTypes.TCP_NUTSSA
                }
            });
        }

        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        public event EventHandler<OnStep2EventArg> OnTcpStep2Handler;
        private readonly List<long> replyIds = new();
        private readonly List<long> connectdIds = new();
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2(OnStep2EventArg e)
        {
            Logger.Instance.Debug($"OnStep2 {e.Data.Ip}->{e.Data.TcpPort}");
            OnTcpStep2Handler?.Invoke(this, e);
            List<Tuple<string, int>> ips = new List<Tuple<string, int>> {
                new Tuple<string, int>(e.Data.LocalIps,e.Data.LocalTcpPort),
                new Tuple<string, int>(e.Data.Ip,e.Data.TcpPort),
            };

            _ = Task.Run(() =>
            {
                long toid = e.Data.Id;
                connectdIds.Add(toid);

                bool success = false;
                int index = 0;
                foreach (var ip in ips)
                {
                    if (!connectdIds.Contains(toid))
                    {
                        break;
                    }

                    Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                    try
                    {
                        targetSocket.Ttl = (short)(RouteLevel + 2);
                        targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        targetSocket.Bind(new IPEndPoint(registerState.LocalInfo.LocalIp, ClientTcpPort));

                        foreach (var device in LibPcapLiveDeviceList.Instance)
                        {
                            device.Open();
                            device.OnPacketArrival += (object s, PacketCapture e) =>
                            {
                                var raw = e.GetPacket();
                                var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data);
                                var ipPacket = packet.Extract<IPPacket>();
                                if (ipPacket != null)
                                {
                                    var tcp = ipPacket.Extract<TcpPacket>();
                                    if (tcp != null)
                                    {
                                        if (tcp.Synchronize == true && tcp.Acknowledgment == false && tcp.DestinationPort == (uint)ip.Item2)
                                        {
                                            eventHandlers.SendTcp(new SendTcpEventArg
                                            {
                                                Data = new RawPacketModel
                                                {
                                                    Data = packet.Bytes,
                                                    FormId = ConnectId,
                                                    ToId = toid,
                                                    LinkLayerType = (byte)raw.LinkLayerType
                                                },
                                                Socket = TcpServer,
                                            });
                                            // device.StopCapture();
                                            //  device.Close();
                                        }
                                        if (tcp.Synchronize == true && tcp.Acknowledgment == true && tcp.DestinationPort == (ushort)registerState.RemoteInfo.TcpPort)
                                        {
                                            Logger.Instance.Debug($"收到ACK包");
                                        }
                                    }
                                }
                            };
                            device.StartCapture();
                        }

                        System.Threading.Thread.Sleep(100);

                        Logger.Instance.Debug($"连接 {ip.Item1}->{ip.Item2}");
                        IAsyncResult result = targetSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2), null, null);
                        _ = result.AsyncWaitHandle.WaitOne(2000, false);
                        if (result.IsCompleted)
                        {
                            Logger.Instance.Debug($"连接 {ip.Item1}->{ip.Item2} 成功");
                            targetSocket.EndConnect(result);
                            tcpServer.BindReceive(targetSocket);
                            SendStep3(new SendStep3EventArg
                            {
                                Socket = targetSocket,
                                ToId = toid
                            });

                            int waitReplyTimes = 10;
                            while (waitReplyTimes > 0)
                            {
                                if (replyIds.Contains(toid))
                                {
                                    _ = replyIds.Remove(toid);

                                    break;
                                }
                                waitReplyTimes--;

                                System.Threading.Thread.Sleep(100);
                            }
                            if (!connectdIds.Contains(toid))
                            {
                                targetSocket.SafeClose();
                                break;
                            }

                            if (waitReplyTimes > 0)
                            {
                                success = true;
                                _ = connectdIds.Remove(toid);
                                break;
                            }
                        }
                        else
                        {
                            Logger.Instance.Debug($"连接 {ip.Item1}->{ip.Item2} 失败");
                            targetSocket.SafeClose();
                            SendStep2Retry(toid);
                        }
                    }
                    catch (SocketException)
                    {
                        targetSocket.SafeClose();
                        targetSocket = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex + "");
                    }

                    index++;
                }

                if (!success)
                {
                    foreach (var device in LibPcapLiveDeviceList.Instance)
                    {
                        device.StopCapture();
                        device.Close();
                    }
                    SendStep2Fail(new OnSendStep2FailEventArg
                    {
                        Id = ConnectId,
                        ToId = toid
                    });
                    connectdIds.Remove(toid);
                }
            });
        }

        public event EventHandler<long> OnSendStep2RetryHandler;
        public void SendStep2Retry(long toid)
        {
            OnSendStep2RetryHandler?.Invoke(this, toid);
        }
        public event EventHandler<OnStep2RetryEventArg> OnStep2RetryHandler;
        public void OnStep2Retry(OnStep2RetryEventArg e)
        {
            OnStep2RetryHandler?.Invoke(this, e);
        }

        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        public event EventHandler<OnSendStep2FailEventArg> OnSendStep2FailHandler;
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep2Fail(OnSendStep2FailEventArg arg)
        {
            OnSendStep2FailHandler?.Invoke(this, arg);
            if (connectTcpCache.TryGetValue(arg.ToId, out ConnectTcpCache cache))
            {
                _ = connectTcpCache.TryRemove(arg.ToId, out _);
                cache?.FailCallback(new ConnectFailModel
                {
                    Msg = "失败",
                    Type = ConnectFailType.ERROR
                });
            }
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = arg.ToId,
                Data = new Step2FailModel
                {
                    FromId = arg.Id,
                    PunchType = PunchHoleTypes.TCP_NUTSSA
                }
            });
        }

        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        public event EventHandler<OnStep2FailEventArg> OnStep2FailHandler;
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2Fail(OnStep2FailEventArg arg)
        {

            OnStep2FailHandler?.Invoke(this, arg);
        }

        public void SendStep2Stop(long toid)
        {
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = toid,
                Data = new Step2StopModel
                {
                    FromId = ConnectId,
                    PunchType = PunchHoleTypes.TCP_NUTSSA

                }
            });
        }

        public void OnStep2Stop(Step2StopModel e)
        {
            connectdIds.Remove(e.FromId);
        }


        /// <summary>
        /// TCP消息，已经连接了对方，发个3告诉对方已连接
        /// </summary>
        public event EventHandler<SendStep3EventArg> OnSendStep3Handler;
        /// <summary>
        /// 开始连接目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep3(SendStep3EventArg arg)
        {
            OnSendStep3Handler?.Invoke(this, arg);
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = arg.Socket,
                ToId = arg.ToId,
                Data = new Step3Model
                {
                    FromId = ConnectId,
                    PunchType = PunchHoleTypes.TCP_NUTSSA
                }
            });
        }


        /// <summary>
        /// 对方连接我了
        /// </summary>
        public event EventHandler<OnStep3EventArg> OnStep3Handler;
        /// <summary>
        /// 对方连接我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep3(OnStep3EventArg arg)
        {
            SendStep4(new SendStep4EventArg
            {
                Socket = arg.Packet.TcpSocket,
                ToId = ConnectId
            });
            OnStep3Handler?.Invoke(this, arg);

        }

        /// <summary>
        /// 回应目标客户端
        /// </summary>
        public event EventHandler<SendStep4EventArg> OnSendStep4Handler;
        /// <summary>
        /// 回应目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep4(SendStep4EventArg arg)
        {
            OnSendStep4Handler?.Invoke(this, arg);
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = arg.Socket,
                ToId = arg.ToId,
                Data = new Step4Model
                {
                    FromId = ConnectId,
                    PunchType = PunchHoleTypes.TCP_NUTSSA
                }
            });
        }

        /// <summary>
        /// 来源客户端回应我了
        /// </summary>
        public event EventHandler<OnStep4EventArg> OnStep4Handler;


        /// <summary>
        /// 来源客户端回应我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep4(OnStep4EventArg arg)
        {
            if (connectTcpCache.TryGetValue(arg.Data.FromId, out ConnectTcpCache cache))
            {
                connectTcpCache.TryRemove(arg.Data.FromId, out _);
                cache?.Callback(arg);
            }
            replyIds.Add(arg.Data.FromId);
            OnStep4Handler?.Invoke(this, arg);
        }

        public event EventHandler<SendStepPacketEventArg> OnSendStepPacketHandler;

        public void SendStepPacket(SendStepPacketEventArg arg)
        {
            OnSendStepPacketHandler?.Invoke(this, arg);
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = arg.ToId,
                Data = new StepPacketModel
                {
                    FromId = arg.FromId,
                    PunchType = PunchHoleTypes.TCP_NUTSSA
                }
            });
        }

        public event EventHandler<OnStepPacketEventArg> OnStepPacketHandler;
        public void OnStepPacket(OnStepPacketEventArg arg)
        {
            Logger.Instance.Debug($"OnStepPacket {arg.Data.FromId}");
            OnStepPacketHandler?.Invoke(this, arg);

            ClientInfo.Get(arg.Data.FromId, out ClientInfo info);
            if (info != null)
            {
                SendPrivate(new ConnectTcpParams
                {
                    Callback = (e) =>
                    {
                        ClientInfo.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket);
                    },
                    FailCallback = (e) =>
                    {
                        Logger.Instance.Error(e.Msg);
                        ClientInfo.OfflineTcp(info.Id);
                    },
                    Id = info.Id,
                    Name = info.Name,
                    Timeout = 300 * 1000
                });
            }
        }

        #endregion
    }
}
