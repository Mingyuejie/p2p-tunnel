﻿using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
using PacketDotNet;
using server;
using server.models;
using server.plugins.register.caching;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.punchHolePlugins.plugins.tcp.nutssa
{
    public class PunchHoleTcpNutssAEventHandles : IPunchHoleTcp
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly IServerRequest serverRequest;
        private readonly ITcpServer tcpServer;
        private readonly RegisterState registerState;
        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;

        public PunchHoleTcpNutssAEventHandles(IServerRequest serverRequest, PunchHoleEventHandles punchHoldEventHandles,
            ITcpServer tcpServer, RegisterState registerState, Config config, IClientInfoCaching clientInfoCaching)
        {
            this.serverRequest = serverRequest;
            this.punchHoldEventHandles = punchHoldEventHandles;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
        }

        private Socket TcpServer => registerState.TcpSocket;
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        public int ClientTcpPort => registerState.LocalInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<long, ConnectTcpCache> connectTcpCache = new();

        #region 连接客户端


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
            connectTcpCache.TryAdd(param.Id, new ConnectTcpCache
            {
                Callback = param.Callback,
                FailCallback = param.FailCallback,
                Time = Helper.GetTimeStamp(),
                Timeout = param.Timeout
            });
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg<Step1Model>
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

        public SimplePushSubHandler<OnStep1EventArg> OnStep1Handler { get; } = new SimplePushSubHandler<OnStep1EventArg>();
        public void OnStep1(OnStep1EventArg e)
        {
            Logger.Instance.Debug($"OnStep1 {e.Data.Id}");
            OnStep1Handler.Push(e);

            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg<Step2Model>
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
        public SimplePushSubHandler<OnStep2EventArg> OnStep2Handler { get; } = new SimplePushSubHandler<OnStep2EventArg>();
        private readonly List<long> replyIds = new();
        private readonly List<long> connectdIds = new();
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2(OnStep2EventArg e)
        {
            Logger.Instance.Debug($"OnStep2 {e.Data.Ip}->{e.Data.TcpPort}");
            OnStep2Handler.Push(e);
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
                        targetSocket.Bind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));

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
                                            serverRequest.SendOnlyTcp(new SendTcpEventArg<RawPacketModel>
                                            {
                                                Data = new RawPacketModel
                                                {
                                                    Data = packet.Bytes,
                                                    FormId = ConnectId,
                                                    ToId = toid,
                                                    LinkLayerType = (byte)raw.LinkLayerType
                                                },
                                                 Path = "RawPacket/excute",
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

        public void SendStep2Retry(long toid)
        {
        }
        public SimplePushSubHandler<OnStep2RetryEventArg> OnStep2RetryHandler { get; } = new SimplePushSubHandler<OnStep2RetryEventArg>();
        public void OnStep2Retry(OnStep2RetryEventArg e)
        {
            OnStep2RetryHandler.Push(e);
        }
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep2Fail(OnSendStep2FailEventArg arg)
        {
            if (connectTcpCache.TryGetValue(arg.ToId, out ConnectTcpCache cache))
            {
                _ = connectTcpCache.TryRemove(arg.ToId, out _);
                cache?.FailCallback(new ConnectFailModel
                {
                    Msg = "失败",
                    Type = ConnectFailType.ERROR
                });
            }
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg<Step2FailModel>
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
        public SimplePushSubHandler<OnStep2FailEventArg> OnStep2FailHandler { get; } = new SimplePushSubHandler<OnStep2FailEventArg>();
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2Fail(OnStep2FailEventArg arg)
        {

            OnStep2FailHandler.Push(arg);
        }

        public void SendStep2Stop(long toid)
        {
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg<Step2StopModel>
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
        /// 开始连接目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep3(SendStep3EventArg arg)
        {
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg<Step3Model>
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
        public SimplePushSubHandler<OnStep3EventArg> OnStep3Handler { get;} = new SimplePushSubHandler<OnStep3EventArg>();
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
            OnStep3Handler.Push(arg);

        }

        /// <summary>
        /// 回应目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep4(SendStep4EventArg arg)
        {
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg<Step4Model>
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
        public SimplePushSubHandler<OnStep4EventArg> OnStep4Handler { get;} = new SimplePushSubHandler<OnStep4EventArg>();
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
            OnStep4Handler.Push(arg);
        }

        public void SendStepPacket(SendStepPacketEventArg arg)
        {
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg<StepPacketModel>
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

        public SimplePushSubHandler<OnStepPacketEventArg> OnStepPacketHandler { get;} = new SimplePushSubHandler<OnStepPacketEventArg>();
        public void OnStepPacket(OnStepPacketEventArg arg)
        {
            Logger.Instance.Debug($"OnStepPacket {arg.Data.FromId}");
            OnStepPacketHandler.Push(arg);

            clientInfoCaching.Get(arg.Data.FromId, out ClientInfo info);
            if (info != null)
            {
                SendPrivate(new ConnectTcpParams
                {
                    Callback = (e) =>
                    {
                        clientInfoCaching.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket);
                    },
                    FailCallback = (e) =>
                    {
                        Logger.Instance.Error(e.Msg);
                        clientInfoCaching.OfflineTcp(info.Id);
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
