using client.service.events;
using client.service.serverPlugins.register;
using common;
using common.extends;
using server;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.serverPlugins.connectClient
{
    public class ConnectClientEventHandles
    {
        private readonly EventHandlers eventHandlers;
        private readonly ITcpServer tcpServer;
        private readonly RegisterState registerState;

        public ConnectClientEventHandles(EventHandlers eventHandlers, ITcpServer tcpServer, RegisterState registerState)
        {
            this.eventHandlers = eventHandlers;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
        }

        private IPEndPoint UdpServer => registerState.UdpAddress;
        private Socket TcpServer => registerState.TcpSocket;
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        public int ClientTcpPort => registerState.RemoteInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        private readonly ConcurrentDictionary<long, ConnectCache> connectCache = new();
        private readonly ConcurrentDictionary<long, ConnectTcpCache> connectTcpCache = new();

        #region 连接客户端  具体流程 看MessageTypes里的描述
        /// <summary>
        /// 发送连接客户端请求消息（给服务器）
        /// </summary>
        public event EventHandler<SendConnectClientEventArg> OnSendConnectClientHandler;
        /// <summary>
        /// 发送连接客户端请求消息（给服务器）
        /// </summary>
        /// <param name="toid"></param>
        public void SendConnectClient(ConnectParams param)
        {
            if (connectCache.ContainsKey(param.Id))
            {
                return;
            }
            connectCache.TryAdd(param.Id, new ConnectCache
            {
                Callback = param.Callback,
                FailCallback = param.FailCallback,
                Time = Helper.GetTimeStamp(),
                Timeout = param.Timeout,
                TryTimes = param.TryTimes
            });

            OnSendConnectClientHandler?.Invoke(this, new SendConnectClientEventArg
            {
                Id = param.Id,
                Name = param.Name
            });

            TryConnect(param.Id);
        }
        private void TryConnect(long id)
        {
            if (connectCache.TryGetValue(id, out ConnectCache cache))
            {

                cache.TryTimes--;
                Console.WriteLine($"{UdpServer}");
                eventHandlers.Send(new SendEventArg
                {
                    Address = UdpServer,
                    Data = new ConnectClientModel
                    {
                        ToId = id,
                        Id = ConnectId
                    }
                });
                Helper.SetTimeout(() =>
                {
                    if (connectCache.TryGetValue(id, out ConnectCache cache))
                    {
                        if (cache.TryTimes > 0)
                        {
                            TryConnect(id);
                        }
                        else
                        {
                            connectCache.TryRemove(id, out _);
                            cache.FailCallback(new ConnectFailModel
                            {
                                Msg = "UDP连接超时",
                                Type = ConnectFailType.TIMEOUT
                            });
                        }
                    }
                }, cache.Timeout);
            }
        }


        /// <summary>
        /// 发送TCP连接客户端请求消息（给服务器）
        /// </summary>
        public event EventHandler<SendConnectClientEventArg> OnSendTcpConnectClientHandler;
        /// <summary>
        /// 发送TCP连接客户端请求消息（给服务器）
        /// </summary>
        /// <param name="toid"></param>
        public void SendTcpConnectClient(ConnectTcpParams param)
        {
            OnSendTcpConnectClientHandler?.Invoke(this, new SendConnectClientEventArg
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

            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = TcpServer,
                Data = new ConnectClientModel
                {
                    ToId = param.Id,
                    Id = ConnectId
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

        /// <summary>
        /// 服务器消息，让某个客户端反向链接我
        /// </summary>
        public event EventHandler<long> OnSendConnectClientReverseHandler;
        /// <summary>
        /// 服务器消息，让某个客户端反向链接我
        /// </summary>
        /// <param name="toid"></param>
        public void SendConnectClientReverse(long id)
        {
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Data = new ConnectClientReverseModel { Id = ConnectId, ToId = id },
                Socket = TcpServer
            });

            OnSendConnectClientReverseHandler?.Invoke(this, id);
        }
        /// <summary>
        /// 服务器消息，某个客户端要我反向链接他
        /// </summary>
        public event EventHandler<OnConnectClientReverseEventArg> OnConnectClientReverseHandler;
        /// <summary>
        /// 服务器消息，某个客户端要我反向链接他
        /// </summary>
        /// <param name="toid"></param>
        public void OnConnectClientReverse(OnConnectClientReverseEventArg arg)
        {
            OnConnectClientReverseHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 服务器消息，某个客户端要跟我连接
        /// </summary>
        public event EventHandler<OnConnectClientStep1EventArg> OnConnectClientStep1Handler;
        public event EventHandler<OnConnectClientStep1ResultEventArg> OnConnectClientStep1ResultHandler;
        /// <summary>
        /// 服务器消息，某个客户端要跟我连接
        /// </summary>
        /// <param name="toid"></param>
        public void OnConnectClientStep1(OnConnectClientStep1EventArg arg)
        {
            OnConnectClientStep1Handler?.Invoke(this, arg);
            //随便给来源客户端发个消息
            List<string> ips = arg.Data.LocalIps.Split(',').Concat(new string[] { arg.Data.Ip }).ToList();
            foreach (string ip in ips)
            {
                eventHandlers.Send(new SendEventArg
                {
                    Address = new IPEndPoint(IPAddress.Parse(ip), arg.Data.Port),
                    Data = new ConnectClientStep1AckModel { Id = ConnectId }
                });
            }
            //告诉服务器我已准备好
            eventHandlers.Send(new SendEventArg
            {
                Address = UdpServer,
                Data = new ConnectClientStep1ResultModel
                {
                    Id = ConnectId,
                    ToId = arg.Data.Id
                }
            });
            OnConnectClientStep1ResultHandler?.Invoke(this, new OnConnectClientStep1ResultEventArg { });

        }
        /// <summary>
        /// 服务器Tcp消息，已拿到对方的信息
        /// </summary>
        public event EventHandler<OnConnectClientStep1EventArg> OnTcpConnectClientStep1Handler;
        public event EventHandler<OnConnectClientStep1ResultEventArg> OnTcpConnectClientStep1ResultHandler;


        /// <summary>
        /// 服务器Tcp消息，已拿到对方的信息
        /// </summary>
        /// <param name="toid"></param>
        public void OnTcpConnectClientStep1(OnConnectClientStep1EventArg e)
        {
            OnTcpConnectClientStep1Handler?.Invoke(this, e);

            List<string> ips = e.Data.LocalIps.Split(',').Concat(new string[] { e.Data.Ip }).ToList();
            foreach (string ip in ips)
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
                        targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), e.Data.LocalTcpPort));
                    }
                    catch (Exception)
                    {
                    }
                    System.Threading.Thread.Sleep(500);
                    targetSocket.SafeClose();
                });
            }

            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = TcpServer,
                Data = new ConnectClientStep1ResultModel
                {
                    Id = ConnectId,
                    ToId = e.Data.Id
                }
            });
            OnTcpConnectClientStep1ResultHandler?.Invoke(this, new OnConnectClientStep1ResultEventArg { });
        }

        /// <summary>
        /// 服务器消息，目标客户端已经准备好
        /// </summary>
        public event EventHandler<OnConnectClientStep2EventArg> OnConnectClientStep2Handler;
        /// <summary>
        /// 服务器消息，目标客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnConnectClientStep2(OnConnectClientStep2EventArg e)
        {
            OnConnectClientStep2Handler?.Invoke(this, e);
            List<string> ips = e.Data.LocalIps.Split(',').Concat(new string[] { e.Data.Ip }).ToList();
            foreach (string ip in ips)
            {
                SendConnectClientStep3(new SendConnectClientStep3EventArg
                {
                    Address = new IPEndPoint(IPAddress.Parse(ip), e.Data.Port),
                    Id = ConnectId
                });
            }
        }

        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        public event EventHandler<OnConnectClientStep2EventArg> OnTcpConnectClientStep2Handler;
        private readonly List<long> replyIds = new();
        private readonly List<long> connectdIds = new();
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnTcpConnectClientStep2(OnConnectClientStep2EventArg e)
        {
            OnTcpConnectClientStep2Handler?.Invoke(this, e);
            string[] ips = e.Data.LocalIps.Split(',').Concat(new string[] { e.Data.Ip }).ToArray();
            _ = Task.Run(() =>
            {
                connectdIds.Add(e.Data.Id);
                bool success = false;
                int length = 5, errLength = 10;
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
                        string ip = length >= ips.Length ? ips[ips.Length - 1] : ips[length];

                        IAsyncResult result = targetSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), e.Data.LocalTcpPort), null, null);
                        _ = result.AsyncWaitHandle.WaitOne(2000, false);
                        if (result.IsCompleted)
                        {

                            targetSocket.EndConnect(result);
                            tcpServer.BindReceive(targetSocket);
                            SendTcpConnectClientStep3(new SendTcpConnectClientStep3EventArg
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
                            SendTcpConnectClientStep2Retry(e.Data.Id);
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
                            SendTcpConnectClientStep2Retry(e.Data.Id);
                            length--;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex + "");
                    }
                }
                if (!success)
                {
                    OnSendTcpConnectClientStep2Fail(new OnSendTcpConnectClientStep2FailEventArg
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
        public event EventHandler<long> OnSendTcpConnectClientStep2RetryHandler;
        /// <summary>
        /// 服务器TCP消息，重试一次
        /// </summary>
        /// <param name="toid"></param>
        public void SendTcpConnectClientStep2Retry(long toid)
        {
            OnSendTcpConnectClientStep2RetryHandler?.Invoke(this, toid);
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = TcpServer,
                Data = new ConnectClientStep2RetryModel
                {
                    Id = ConnectId,
                    ToId = toid
                }
            });
        }
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        public event EventHandler<OnConnectClientStep2RetryEventArg> OnTcpConnectClientStep2RetryHandler;
        /// <summary>
        /// 服务器TCP消息，来源客户端已经准备好
        /// </summary>
        /// <param name="toid"></param>
        public void OnTcpConnectClientStep2Retry(OnConnectClientStep2RetryEventArg e)
        {
            OnTcpConnectClientStep2RetryHandler?.Invoke(this, e);
            Task.Run(() =>
            {
                //随便给目标客户端发个低TTL消息
                using Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    Ttl = (short)(RouteLevel + 2)
                };
                targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                targetSocket.Bind(new IPEndPoint(registerState.LocalInfo.LocalIp, ClientTcpPort));
                targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(e.Data.Ip), e.Data.LocalTcpPort));
                System.Threading.Thread.Sleep(500);
                targetSocket.SafeClose();
            });
        }


        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        public event EventHandler<OnSendTcpConnectClientStep2FailEventArg> OnSendTcpConnectClientStep2FailHandler;
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void OnSendTcpConnectClientStep2Fail(OnSendTcpConnectClientStep2FailEventArg arg)
        {
            OnSendTcpConnectClientStep2FailHandler?.Invoke(this, arg);
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = TcpServer,
                Data = new ConnectClientStep2FailModel
                {
                    Id = arg.Id,
                    ToId = arg.ToId
                }
            });
        }

        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        public event EventHandler<OnTcpConnectClientStep2FailEventArg> OnTcpConnectClientStep2FailHandler;
        /// <summary>
        /// 服务器TCP消息，链接失败
        /// </summary>
        /// <param name="toid"></param>
        public void OnTcpConnectClientStep2Fail(OnTcpConnectClientStep2FailEventArg arg)
        {
            if (connectTcpCache.TryGetValue(arg.Data.Id, out ConnectTcpCache cache))
            {
                _ = connectTcpCache.TryRemove(arg.Data.Id, out _);
                cache?.FailCallback(new ConnectFailModel
                {
                    Msg = "失败",
                    Type = ConnectFailType.ERROR
                });
            }
            OnTcpConnectClientStep2FailHandler?.Invoke(this, arg);
        }

        public void SendTcpConnectClientStep2StopMessage(long toid)
        {
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = TcpServer,
                Data = new ConnectClientStep2StopModel
                {
                    Id = ConnectId,
                    ToId = toid
                }
            });
        }

        public void OnTcpConnectClientStep2StopMessage(ConnectClientStep2StopModel e)
        {
            connectdIds.Remove(e.Id);
        }


        /// <summary>
        /// 开始连接目标客户端
        /// </summary>
        public event EventHandler<SendConnectClientStep3EventArg> OnSendConnectClientStep3Handler;
        /// <summary>
        /// 开始连接目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendConnectClientStep3(SendConnectClientStep3EventArg arg)
        {
            OnSendConnectClientStep3Handler?.Invoke(this, arg);
            eventHandlers.Send(new SendEventArg
            {
                Address = arg.Address,
                Data = new ConnectClientStep3Model
                {
                    Id = arg.Id
                }
            });

        }
        /// <summary>
        /// TCP消息，已经连接了对方，发个3告诉对方已连接
        /// </summary>
        public event EventHandler<SendTcpConnectClientStep3EventArg> OnSendTcpConnectClientStep3Handler;
        /// <summary>
        /// 开始连接目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendTcpConnectClientStep3(SendTcpConnectClientStep3EventArg arg)
        {

            OnSendTcpConnectClientStep3Handler?.Invoke(this, arg);
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = arg.Socket,
                Data = new ConnectClientStep3Model
                {
                    Id = arg.Id
                }
            });
        }

        /// <summary>
        /// 来源客户端开始连接我了
        /// </summary>
        public event EventHandler<OnConnectClientStep3EventArg> OnConnectClientStep3Handler;
        /// <summary>
        /// 来源客户端开始连接我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnConnectClientStep3(OnConnectClientStep3EventArg e)
        {
            OnConnectClientStep3Handler?.Invoke(this, e);
            SendConnectClientStep4Message(new SendConnectClientStep4EventArg
            {
                Address = e.Packet.SourcePoint,
                Id = ConnectId
            });
        }
        /// <summary>
        /// 对方连接我了
        /// </summary>
        public event EventHandler<OnConnectClientStep3EventArg> OnTcpConnectClientStep3Handler;
        /// <summary>
        /// 对方连接我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnTcpConnectClientStep3(OnConnectClientStep3EventArg arg)
        {
            if (connectTcpCache.TryGetValue(arg.Data.Id, out ConnectTcpCache cache))
            {
                connectTcpCache.TryRemove(arg.Data.Id, out _);
                cache?.Callback(arg);
                SendTcpConnectClientStep4(new SendTcpConnectClientStep4EventArg
                {
                    Socket = arg.Packet.TcpSocket,
                    Id = registerState.RemoteInfo.ConnectId
                });
            }
            OnTcpConnectClientStep3Handler?.Invoke(this, arg);

        }

        /// <summary>
        /// 回应来源客户端
        /// </summary>
        public event EventHandler<SendConnectClientStep4EventArg> OnSendConnectClientStep4Handler;
        /// <summary>
        /// 回应来源客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendConnectClientStep4Message(SendConnectClientStep4EventArg arg)
        {
            OnSendConnectClientStep4Handler?.Invoke(this, arg);
            eventHandlers.Send(new SendEventArg
            {
                Address = arg.Address,
                Data = new ConnectClientStep4Model
                {
                    Id = arg.Id
                }
            });
        }
        /// <summary>
        /// 回应目标客户端
        /// </summary>
        public event EventHandler<SendTcpConnectClientStep4EventArg> OnSendTcpConnectClientStep4Handler;
        /// <summary>
        /// 回应目标客户端
        /// </summary>
        /// <param name="toid"></param>
        public void SendTcpConnectClientStep4(SendTcpConnectClientStep4EventArg arg)
        {

            OnSendTcpConnectClientStep4Handler?.Invoke(this, arg);
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = arg.Socket,
                Data = new ConnectClientStep4Model
                {
                    Id = arg.Id
                }
            });
        }

        /// <summary>
        /// 目标客户端回应我了
        /// </summary>
        public event EventHandler<OnConnectClientStep4EventArg> OnConnectClientStep4Handler;
        /// <summary>
        /// 目标客户端回应我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnConnectClientStep4(OnConnectClientStep4EventArg arg)
        {
            if (connectCache.TryRemove(arg.Data.Id, out ConnectCache cache))
            {
                cache?.Callback(arg);
            }
            OnConnectClientStep4Handler?.Invoke(this, arg);
        }
        /// <summary>
        /// 来源客户端回应我了
        /// </summary>
        public event EventHandler<OnConnectClientStep4EventArg> OnTcpConnectClientStep4Handler;
        /// <summary>
        /// 来源客户端回应我了
        /// </summary>
        /// <param name="toid"></param>
        public void OnTcpConnectClientStep4(OnConnectClientStep4EventArg arg)
        {
            replyIds.Add(arg.Data.Id);
            OnTcpConnectClientStep4Handler?.Invoke(this, arg);
        }

        #endregion
    }


    #region 连接客户端model

    public class SendConnectClientEventArg : EventArgs
    {
        public string Name { get; set; }
        public long Id { get; set; }
    }

    public class OnConnectClientReverseEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public ConnectClientReverseModel Data { get; set; }
    }


    public class OnConnectClientStep1EventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public ConnectClientStep1Model Data { get; set; }
    }

    public class OnConnectClientStep1ResultEventArg : EventArgs
    {
    }

    public class OnConnectClientStep2EventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public ConnectClientStep2Model Data { get; set; }
    }

    public class OnConnectClientStep2RetryEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public ConnectClientStep2RetryModel Data { get; set; }
    }

    public class OnSendTcpConnectClientStep2FailEventArg : EventArgs
    {
        public long ToId { get; set; }
        public long Id { get; set; }
    }
    public class OnTcpConnectClientStep2FailEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public ConnectClientStep2FailModel Data { get; set; }
    }
    public class SendConnectClientStep3EventArg : EventArgs
    {
        /// <summary>
        /// 目标地址
        /// </summary>
        public IPEndPoint Address { get; set; }
        /// <summary>
        /// 我的id
        /// </summary>
        public long Id { get; set; }
    }
    public class SendTcpConnectClientStep3EventArg : EventArgs
    {
        /// <summary>
        /// 目标对象
        /// </summary>
        public Socket Socket { get; set; }
        /// <summary>
        /// 我的id
        /// </summary>
        public long Id { get; set; }
    }
    public class OnConnectClientStep3EventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public ConnectClientStep3Model Data { get; set; }
    }

    public class SendConnectClientStep4EventArg : EventArgs
    {
        /// <summary>
        /// 目标地址
        /// </summary>
        public IPEndPoint Address { get; set; }
        /// <summary>
        /// 我的id
        /// </summary>
        public long Id { get; set; }
    }
    public class SendTcpConnectClientStep4EventArg : EventArgs
    {
        /// <summary>
        /// 目标对象
        /// </summary>
        public Socket Socket { get; set; }
        /// <summary>
        /// 我的id
        /// </summary>
        public long Id { get; set; }
    }
    public class OnConnectClientStep4EventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public ConnectClientStep4Model Data { get; set; }
    }

    #endregion


    public class ConnectParams
    {
        public long Id { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public string Name { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5 * 1000;
        public Action<OnConnectClientStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public int Timeout { get; set; } = 5 * 1000;
        public Action<OnConnectClientStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectTcpParams
    {
        public long Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int Timeout { get; set; } = 15 * 1000;
        public Action<OnConnectClientStep3EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectTcpCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public int Timeout { get; set; } = 15 * 1000;
        public Action<OnConnectClientStep3EventArg> Callback { get; set; } = null;
        public Action<ConnectFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectFailModel
    {
        public ConnectFailType Type { get; set; } = ConnectFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }

    public enum ConnectFailType
    {
        ERROR, TIMEOUT
    }
}
