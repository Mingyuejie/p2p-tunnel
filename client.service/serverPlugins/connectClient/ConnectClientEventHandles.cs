using client.service.events;
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
        private static readonly Lazy<ConnectClientEventHandles> lazy = new Lazy<ConnectClientEventHandles>(() => new ConnectClientEventHandles());
        public static ConnectClientEventHandles Instance => lazy.Value;

        private ConnectClientEventHandles()
        {

        }

        private EventHandlers EventHandlers => EventHandlers.Instance;
        private IPEndPoint UdpServer => EventHandlers.UdpServer;
        private Socket TcpServer => EventHandlers.TcpServer;
        private long ConnectId => EventHandlers.ConnectId;

        public int ClientTcpPort => EventHandlers.ClientTcpPort;
        public int RouteLevel => EventHandlers.RouteLevel;

        private readonly ConcurrentDictionary<long, ConnectMessageCache> connectCache = new();
        private readonly ConcurrentDictionary<long, ConnectTcpMessageCache> connectTcpCache = new();

        #region 连接客户端  具体流程 看MessageTypes里的描述
        /// <summary>
        /// 发送连接客户端请求消息（给服务器）
        /// </summary>
        public event EventHandler<SendConnectClientEventArg> OnSendConnectClientMessageHandler;
        /// <summary>
        /// 发送连接客户端请求消息（给服务器）
        /// </summary>
        /// <param name="toid"></param>
        public void SendConnectClientMessage(ConnectParams param)
        {
            if (connectCache.ContainsKey(param.Id))
            {
                return;
            }
            connectCache.TryAdd(param.Id, new ConnectMessageCache
            {
                Callback = param.Callback,
                FailCallback = param.FailCallback,
                Time = Helper.GetTimeStamp(),
                Timeout = param.Timeout,
                TryTimes = param.TryTimes
            });

            OnSendConnectClientMessageHandler?.Invoke(this, new SendConnectClientEventArg
            {
                Id = param.Id,
                Name = param.Name
            });

            TryConnect(param.Id);
        }
        private void TryConnect(long id)
        {
            if (connectCache.TryGetValue(id, out ConnectMessageCache cache))
            {

                cache.TryTimes--;
                EventHandlers.SendMessage(new SendMessageEventArg
                {
                    Address = UdpServer,
                    Data = new MessageConnectClientModel
                    {
                        ToId = id,
                        Id = ConnectId
                    }
                });
                Helper.SetTimeout(() =>
                {
                    if (connectCache.TryGetValue(id, out ConnectMessageCache cache))
                    {
                        if (cache.TryTimes > 0)
                        {
                            TryConnect(id);
                        }
                        else
                        {
                            connectCache.TryRemove(id, out _);
                            cache.FailCallback(new ConnectMessageFailModel
                            {
                                Msg = "UDP连接超时",
                                Type = ConnectMessageFailType.TIMEOUT
                            });
                        }
                    }
                }, cache.Timeout);
            }
        }


        /// <summary>
        /// 发送TCP连接客户端请求消息（给服务器）
        /// </summary>
        public event EventHandler<SendConnectClientEventArg> OnSendTcpConnectClientMessageHandler;
        /// <summary>
        /// 发送TCP连接客户端请求消息（给服务器）
        /// </summary>
        /// <param name="toid"></param>
        public void SendTcpConnectClientMessage(ConnectTcpParams param)
        {
            OnSendTcpConnectClientMessageHandler?.Invoke(this, new SendConnectClientEventArg
            {
                Id = param.Id,
                Name = param.Name
            });

            connectTcpCache.TryAdd(param.Id, new ConnectTcpMessageCache
            {
                Callback = param.Callback,
                FailCallback = param.FailCallback,
                Time = Helper.GetTimeStamp(),
                Timeout = param.Timeout
            });

            EventHandlers.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = TcpServer,
                Data = new MessageConnectClientModel
                {
                    ToId = param.Id,
                    Id = ConnectId
                }
            });

            long id = param.Id;
            Helper.SetTimeout(() =>
            {
                if (connectTcpCache.TryGetValue(id, out ConnectTcpMessageCache cache))
                {
                    connectTcpCache.TryRemove(id, out _);
                    cache?.FailCallback(new ConnectMessageFailModel
                    {
                        Msg = "TCP连接超时",
                        Type = ConnectMessageFailType.TIMEOUT
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
        public void SendConnectClientReverseMessage(long id)
        {
            EventHandlers.Instance.SendTcpMessage(new SendTcpMessageEventArg
            {
                Data = new MessageConnectClientReverseModel { Id = ConnectId, ToId = id },
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
        public void OnConnectClientReverseMessage(OnConnectClientReverseEventArg arg)
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
        public void OnConnectClientStep1Message(OnConnectClientStep1EventArg arg)
        {
            OnConnectClientStep1Handler?.Invoke(this, arg);
            //随便给来源客户端发个消息
            List<string> ips = arg.Data.LocalIps.Split(',').Concat(new string[] { arg.Data.Ip }).ToList();
            foreach (string ip in ips)
            {
                EventHandlers.SendMessage(new SendMessageEventArg
                {
                    Address = new IPEndPoint(IPAddress.Parse(ip), arg.Data.Port),
                    Data = new MessageConnectClientStep1AckModel { Id = ConnectId }
                });
            }
            //告诉服务器我已准备好
            EventHandlers.SendMessage(new SendMessageEventArg
            {
                Address = null,
                Data = new MessageConnectClientStep1ResultModel
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
        public void OnTcpConnectClientStep1Message(OnConnectClientStep1EventArg e)
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
                        targetSocket.Bind(new IPEndPoint(AppShareData.Instance.LocalIp, ClientTcpPort));
                        targetSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), e.Data.LocalTcpPort));
                    }
                    catch (Exception)
                    {
                    }
                    System.Threading.Thread.Sleep(100);
                    targetSocket.SafeClose();
                });
            }

            EventHandlers.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = TcpServer,
                Data = new MessageConnectClientStep1ResultModel
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
        public void OnConnectClientStep2Message(OnConnectClientStep2EventArg e)
        {
            OnConnectClientStep2Handler?.Invoke(this, e);
            List<string> ips = e.Data.LocalIps.Split(',').Concat(new string[] { e.Data.Ip }).ToList();
            foreach (string ip in ips)
            {
                SendConnectClientStep3Message(new SendConnectClientStep3EventArg
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
        public void OnTcpConnectClientStep2Message(OnConnectClientStep2EventArg e)
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
                        targetSocket.Bind(new IPEndPoint(AppShareData.Instance.LocalIp, ClientTcpPort));
                        string ip = length >= ips.Length ? ips[ips.Length - 1] : ips[length];

                        IAsyncResult result = targetSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), e.Data.LocalTcpPort), null, null);
                        _ = result.AsyncWaitHandle.WaitOne(2000, false);
                        if (result.IsCompleted)
                        {

                            targetSocket.EndConnect(result);
                            TCPServer.Instance.BindReceive(targetSocket);
                            SendTcpConnectClientStep3Message(new SendTcpConnectClientStep3EventArg
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
                            SendTcpConnectClientStep2RetryMessage(e.Data.Id);
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
                            SendTcpConnectClientStep2RetryMessage(e.Data.Id);
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
                    OnSendTcpConnectClientStep2FailMessage(new OnSendTcpConnectClientStep2FailEventArg
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
        public void SendTcpConnectClientStep2RetryMessage(long toid)
        {
            OnSendTcpConnectClientStep2RetryHandler?.Invoke(this, toid);
            EventHandlers.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = TcpServer,
                Data = new MessageConnectClientStep2RetryModel
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
        public void OnTcpConnectClientStep2RetryMessage(OnConnectClientStep2RetryEventArg e)
        {
            OnTcpConnectClientStep2RetryHandler?.Invoke(this, e);
            Task.Run(() =>
            {
                //随便给目标客户端发个低TTL消息
                using Socket targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.Ttl = (short)(RouteLevel + 2);
                targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                targetSocket.Bind(new IPEndPoint(AppShareData.Instance.LocalIp, ClientTcpPort));
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
        public void OnSendTcpConnectClientStep2FailMessage(OnSendTcpConnectClientStep2FailEventArg arg)
        {
            OnSendTcpConnectClientStep2FailHandler?.Invoke(this, arg);
            EventHandlers.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = TcpServer,
                Data = new MessageConnectClientStep2FailModel
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
        public void OnTcpConnectClientStep2FailMessage(OnTcpConnectClientStep2FailEventArg arg)
        {
            if (connectTcpCache.TryGetValue(arg.Data.Id, out ConnectTcpMessageCache cache))
            {
                _ = connectTcpCache.TryRemove(arg.Data.Id, out _);
                cache?.FailCallback(new ConnectMessageFailModel
                {
                    Msg = "失败",
                    Type = ConnectMessageFailType.ERROR
                });
            }
            OnTcpConnectClientStep2FailHandler?.Invoke(this, arg);
        }

        public void SendTcpConnectClientStep2StopMessage(long toid)
        {
            EventHandlers.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = TcpServer,
                Data = new MessageConnectClientStep2StopModel
                {
                    Id = ConnectId,
                    ToId = toid
                }
            });
        }

        public void OnTcpConnectClientStep2StopMessage(MessageConnectClientStep2StopModel e)
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
        public void SendConnectClientStep3Message(SendConnectClientStep3EventArg arg)
        {
            OnSendConnectClientStep3Handler?.Invoke(this, arg);
            EventHandlers.SendMessage(new SendMessageEventArg
            {
                Address = arg.Address,
                Data = new MessageConnectClientStep3Model
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
        public void SendTcpConnectClientStep3Message(SendTcpConnectClientStep3EventArg arg)
        {

            OnSendTcpConnectClientStep3Handler?.Invoke(this, arg);
            EventHandlers.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = arg.Socket,
                Data = new MessageConnectClientStep3Model
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
        public void OnConnectClientStep3Message(OnConnectClientStep3EventArg e)
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
        public void OnTcpConnectClientStep3Message(OnConnectClientStep3EventArg arg)
        {
            if (connectTcpCache.TryGetValue(arg.Data.Id, out ConnectTcpMessageCache cache))
            {
                connectTcpCache.TryRemove(arg.Data.Id, out _);
                cache?.Callback(arg);
                SendTcpConnectClientStep4Message(new SendTcpConnectClientStep4EventArg
                {
                    Socket = arg.Packet.TcpSocket,
                    Id = AppShareData.Instance.ConnectId
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
            EventHandlers.SendMessage(new SendMessageEventArg
            {
                Address = arg.Address,
                Data = new MessageConnectClientStep4Model
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
        public void SendTcpConnectClientStep4Message(SendTcpConnectClientStep4EventArg arg)
        {

            OnSendTcpConnectClientStep4Handler?.Invoke(this, arg);
            EventHandlers.SendTcpMessage(new SendTcpMessageEventArg
            {
                Socket = arg.Socket,
                Data = new MessageConnectClientStep4Model
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
        public void OnConnectClientStep4Message(OnConnectClientStep4EventArg arg)
        {
            if (connectCache.TryRemove(arg.Data.Id, out ConnectMessageCache cache))
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
        public void OnTcpConnectClientStep4Message(OnConnectClientStep4EventArg arg)
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
        public MessageConnectClientReverseModel Data { get; set; }
    }


    public class OnConnectClientStep1EventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public MessageConnectClientStep1Model Data { get; set; }
    }

    public class OnConnectClientStep1ResultEventArg : EventArgs
    {
    }

    public class OnConnectClientStep2EventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public MessageConnectClientStep2Model Data { get; set; }
    }

    public class OnConnectClientStep2RetryEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public MessageConnectClientStep2RetryModel Data { get; set; }
    }

    public class OnSendTcpConnectClientStep2FailEventArg : EventArgs
    {
        public long ToId { get; set; }
        public long Id { get; set; }
    }
    public class OnTcpConnectClientStep2FailEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public MessageConnectClientStep2FailModel Data { get; set; }
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
        public MessageConnectClientStep3Model Data { get; set; }
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
        public MessageConnectClientStep4Model Data { get; set; }
    }

    #endregion


    public class ConnectParams
    {
        public long Id { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public string Name { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5 * 1000;
        public Action<OnConnectClientStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectMessageFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectMessageCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public int Timeout { get; set; } = 5 * 1000;
        public Action<OnConnectClientStep4EventArg> Callback { get; set; } = null;
        public Action<ConnectMessageFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectTcpParams
    {
        public long Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int Timeout { get; set; } = 15 * 1000;
        public Action<OnConnectClientStep3EventArg> Callback { get; set; } = null;
        public Action<ConnectMessageFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectTcpMessageCache
    {
        public long Time { get; set; } = 0;
        public int TryTimes { get; set; } = 10;
        public int Timeout { get; set; } = 15 * 1000;
        public Action<OnConnectClientStep3EventArg> Callback { get; set; } = null;
        public Action<ConnectMessageFailModel> FailCallback { get; set; } = null;
    }

    public class ConnectMessageFailModel
    {
        public ConnectMessageFailType Type { get; set; } = ConnectMessageFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }

    public enum ConnectMessageFailType
    {
        ERROR, TIMEOUT
    }
}
