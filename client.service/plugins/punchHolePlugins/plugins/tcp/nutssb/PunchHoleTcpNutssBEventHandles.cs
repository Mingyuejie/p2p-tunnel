using client.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.register;
using common;
using common.extends;
using server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.punchHolePlugins.plugins.tcp.nutssb
{
    public class PunchHoleTcpNutssBEventHandles : IPunchHoleTcp
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly ITcpServer tcpServer;
        private readonly RegisterState registerState;
        private readonly Config config;

        public PunchHoleTcpNutssBEventHandles(PunchHoleEventHandles punchHoldEventHandles, ITcpServer tcpServer,
            RegisterState registerState, Config config)
        {
            this.punchHoldEventHandles = punchHoldEventHandles;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
            this.config = config;
        }

        private Socket TcpServer => registerState.TcpSocket;
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        public int ClientTcpPort => registerState.LocalInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<long, ConnectTcpCache> connectTcpCache = new();


        /// <summary>
        /// 发送TCP连接客户端请求消息（给服务器）
        /// </summary>
        /// <param name="toid"></param>
        public void Send(ConnectTcpParams param)
        {
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
            OnStep1Handler.Push(e);

            List<Tuple<string, int>> ips = new List<Tuple<string, int>> {
             new Tuple<string, int>(e.Data.LocalIps,e.Data.LocalTcpPort),
                new Tuple<string, int>(e.Data.Ip,e.Data.TcpPort),
            };
            foreach (Tuple<string, int> ip in ips)
            {
                _ = Task.Run(() =>
                {
                    //随便给目标客户端发个低TTL消息
                    using Socket targetSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.Ttl = (short)(RouteLevel + 2);
                        targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        targetSocket.Bind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                        targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2));
                    }
                    catch (Exception)
                    {
                    }
                    System.Threading.Thread.Sleep(500);
                    targetSocket.SafeClose();
                });
            }

            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = e.Data.Id,
                Data = new Step2Model
                {
                    FromId = ConnectId
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
            OnStep2Handler.Push(e);
            List<Tuple<string, int>> ips = new List<Tuple<string, int>> {
              new Tuple<string, int>(e.Data.LocalIps,e.Data.LocalTcpPort),
                new Tuple<string, int>(e.Data.Ip,e.Data.TcpPort),
            };

            _ = Task.Run(() =>
            {
                connectdIds.Add(e.Data.Id);
                bool success = false;
                int length = 5, index = 0, errLength = 10;
                int interval = 0;
                while (length > 0 && errLength > 0)
                {
                    if (!connectdIds.Contains(e.Data.Id))
                    {
                        break;
                    }
                    if (interval > 0)
                    {
                        System.Threading.Thread.Sleep(interval);
                        interval = 0;
                    }

                    Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        targetSocket.Bind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                        Tuple<string, int> ip = index >= ips.Count ? ips[ips.Count - 1] : ips[index];

                        IAsyncResult result = targetSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2), null, null);
                        _ = result.AsyncWaitHandle.WaitOne(2000, false);
                        if (result.IsCompleted)
                        {
                            Logger.Instance.Debug($"{ip.Item1}:{ip.Item2} 连接成功");
                            targetSocket.EndConnect(result);
                            tcpServer.BindReceive(targetSocket);
                            SendStep3(new SendStep3EventArg
                            {
                                Socket = targetSocket,
                                ToId = ConnectId
                            });

                            int waitReplyTimes = 10;
                            while (waitReplyTimes > 0)
                            {
                                if (replyIds.Contains(e.Data.Id))
                                {
                                    _ = replyIds.Remove(e.Data.Id);

                                    break;
                                }
                                waitReplyTimes--;

                                System.Threading.Thread.Sleep(500);
                            }
                            if (!connectdIds.Contains(e.Data.Id))
                            {
                                targetSocket.SafeClose();
                                break;
                            }

                            if (waitReplyTimes > 0)
                            {
                                success = true;
                                _ = connectdIds.Remove(e.Data.Id);
                                break;
                            }
                        }
                        else
                        {
                            Logger.Instance.Debug($"{ip.Item1}:{ip.Item2} 连接失败");
                            targetSocket.SafeClose();
                            interval = 300;
                            SendStep2Retry(e.Data.Id);
                            length--;
                        }
                    }
                    catch (SocketException ex)
                    {
                        targetSocket.SafeClose();
                        targetSocket = null;
                        if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        {
                            interval = 2000;
                            errLength--;
                        }
                        else
                        {
                            interval = 100;
                            SendStep2Retry(e.Data.Id);
                            length--;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex + "");
                    }

                    index++;
                }
                if (!success)
                {
                    SendStep2Fail(new OnSendStep2FailEventArg
                    {
                        Id = ConnectId,
                        ToId = e.Data.Id
                    });
                    connectdIds.Remove(e.Data.Id);
                }
            });
        }

        /// <summary>
        /// 服务器TCP消息，重试一次
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep2Retry(long toid)
        {
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = toid,
                Data = new Step2TryModel
                {
                    FromId = ConnectId,
                }
            });
        }
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        public SimplePushSubHandler<OnStep2RetryEventArg> OnStep2RetryHandler { get; } = new SimplePushSubHandler<OnStep2RetryEventArg>();
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2Retry(OnStep2RetryEventArg e)
        {
            OnStep2RetryHandler.Push(e);
            Task.Run(() =>
            {
                Logger.Instance.Debug($"低ttl {e.Data.Ip}:{ e.Data.TcpPort}");
                //随便给目标客户端发个低TTL消息
                using Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.Ttl = (short)(RouteLevel + 5);
                targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                targetSocket.Bind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(e.Data.Ip), e.Data.TcpPort));
                //System.Threading.Thread.Sleep(500);
                targetSocket.SafeClose();
            });
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
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = arg.ToId,
                Data = new Step2FailModel
                {
                    FromId = arg.Id
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
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = TcpServer,
                ToId = toid,
                Data = new Step2StopModel
                {
                    FromId = ConnectId,

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
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = arg.Socket,
                Data = new Step3Model
                {
                    FromId = ConnectId
                }
            });
        }
        /// <summary>
        /// 对方连接我了
        /// </summary>
        public SimplePushSubHandler<OnStep3EventArg> OnStep3Handler { get; } = new SimplePushSubHandler<OnStep3EventArg>();
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
            punchHoldEventHandles.SendTcp(new SendPunchHoleTcpArg
            {
                Socket = arg.Socket,
                Data = new Step4Model
                {
                    FromId = ConnectId
                }
            });
        }

        /// <summary>
        /// 来源客户端回应我了
        /// </summary>
        public SimplePushSubHandler<OnStep4EventArg> OnStep4Handler { get; } = new SimplePushSubHandler<OnStep4EventArg>();
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


        public SimplePushSubHandler<OnStepPacketEventArg> OnStepPacketHandler { get; } = new SimplePushSubHandler<OnStepPacketEventArg>();
        public void SendStepPacket(SendStepPacketEventArg arg)
        {
        }

        public void OnStepPacket(OnStepPacketEventArg arg)
        {
            OnStepPacketHandler.Push(arg);
        }
    }
}
