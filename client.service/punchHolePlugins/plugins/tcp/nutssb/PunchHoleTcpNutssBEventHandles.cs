using client.service.events;
using client.service.serverPlugins.register;
using common;
using common.extends;
using ProtoBuf;
using server;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.punchHolePlugins.plugins.tcp.nutssb
{
    public class PunchHoleTcpNutssBEventHandles : IPunchHoleTcp
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly ITcpServer tcpServer;
        private readonly RegisterState registerState;

        public PunchHoleTcpNutssBEventHandles(PunchHoleEventHandles punchHoldEventHandles, ITcpServer tcpServer, RegisterState registerState)
        {
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
            OnStep1Handler?.Invoke(this, e);

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
                        targetSocket.Bind(new IPEndPoint(registerState.LocalInfo.LocalIp, ClientTcpPort));
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
        public event EventHandler<OnStep2EventArg> OnTcpStep2Handler;
        private readonly List<long> replyIds = new();
        private readonly List<long> connectdIds = new();
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2(OnStep2EventArg e)
        {
            OnTcpStep2Handler?.Invoke(this, e);
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
                        targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        targetSocket.Bind(new IPEndPoint(registerState.LocalInfo.LocalIp, ClientTcpPort));
                        Tuple<string, int> ip = index >= ips.Count ? ips[ips.Count - 1] : ips[index];

                        IAsyncResult result = targetSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip.Item1), ip.Item2), null, null);
                        _ = result.AsyncWaitHandle.WaitOne(2000, false);
                        if (result.IsCompleted)
                        {
                            targetSocket.EndConnect(result);
                            tcpServer.BindReceive(targetSocket);
                            SendStep3(new SendStep3EventArg
                            {
                                Socket = targetSocket,
                                Id = ConnectId
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
                            targetSocket.SafeClose();
                            interval = 2000;
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
        public event EventHandler<long> OnSendStep2RetryHandler;
        /// <summary>
        /// 服务器TCP消息，重试一次
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep2Retry(long toid)
        {
            OnSendStep2RetryHandler?.Invoke(this, toid);
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
        public event EventHandler<OnStep2RetryEventArg> OnStep2RetryHandler;
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2Retry(OnStep2RetryEventArg e)
        {
            OnStep2RetryHandler?.Invoke(this, e);
            Task.Run(() =>
            {
                //随便给目标客户端发个低TTL消息
                using Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    Ttl = (short)(RouteLevel + 2)
                };
                targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                targetSocket.Bind(new IPEndPoint(registerState.LocalInfo.LocalIp, ClientTcpPort));
                targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(e.Data.Ip), e.Data.TcpPort));
                System.Threading.Thread.Sleep(500);
                targetSocket.SafeClose();
            });
        }


        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        public event EventHandler<OnSendStep2FailEventArg> OnSendTcpStep2FailHandler;
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void SendStep2Fail(OnSendStep2FailEventArg arg)
        {
            OnSendTcpStep2FailHandler?.Invoke(this, arg);
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
        public event EventHandler<OnTcpStep2FailEventArg> OnStep2FailHandler;
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void OnStep2Fail(OnTcpStep2FailEventArg arg)
        {
            if (connectTcpCache.TryGetValue(arg.Data.FromId, out ConnectTcpCache cache))
            {
                _ = connectTcpCache.TryRemove(arg.Data.FromId, out _);
                cache?.FailCallback(new ConnectFailModel
                {
                    Msg = "失败",
                    Type = ConnectFailType.ERROR
                });
            }
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
                Data = new Step3Model
                {
                    FromId = arg.Id
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
            if (connectTcpCache.TryGetValue(arg.Data.FromId, out ConnectTcpCache cache))
            {
                connectTcpCache.TryRemove(arg.Data.FromId, out _);
                cache?.Callback(arg);
                SendStep4(new SendStep4EventArg
                {
                    Socket = arg.Packet.TcpSocket,
                    Id = ConnectId
                });
            }
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
                Data = new Step4Model
                {
                    FromId = ConnectId
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
            replyIds.Add(arg.Data.FromId);
            OnStep4Handler?.Invoke(this, arg);
        }

        #endregion
    }
}
