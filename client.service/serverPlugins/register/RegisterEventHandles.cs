using client.service.events;
using common;
using server;
using server.model;
using server.models;
using System;
using System.Net;
using System.Net.Sockets;

namespace client.service.serverPlugins.register
{
    public class RegisterEventHandles
    {
        private RegisterParams requestCache;
        private long requestCacheId = 0;

        private readonly EventHandlers eventHandlers;
        private readonly RegisterState registerState;
        private readonly IUdpServer udpServer;
        private readonly ITcpServer tcpServer;

        public RegisterEventHandles(EventHandlers eventHandlers, RegisterState registerState, IUdpServer udpServer, ITcpServer tcpServer)
        {
            this.eventHandlers = eventHandlers;
            this.registerState = registerState;
            this.udpServer = udpServer;
            this.tcpServer = tcpServer;
        }

        private IPEndPoint UdpServer => registerState.UdpAddress;
        private Socket TcpServer => registerState.TcpSocket;
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        public int ClientTcpPort => registerState.RemoteInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        /// <summary>
        /// 发送退出消息
        /// </summary>
        public event EventHandler<SendEventArg> OnSendExitMessageHandler;
        /// <summary>
        /// 发送退出消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendExitMessage()
        {
            requestCache = null;

            SendEventArg arg = new()
            {
                Address = UdpServer,
                Data = new ExitModel
                {
                    Id = ConnectId,
                }
            };

            if (UdpServer != null)
            {
                udpServer.Send(new RecvQueueModel<IModelBase>
                {
                    Address = arg.Address,
                    Data = arg.Data
                });
                udpServer.Stop();
                tcpServer.Stop();

                SendRegisterStateChange(new RegisterEventArg
                {
                    State = false
                });
                SendRegisterTcpStateChange(new RegisterTcpEventArg
                {
                    State = false
                });
            }
            OnSendExitMessageHandler?.Invoke(this, arg);
            eventHandlers.Sequence = 0;

            Helper.FlushMemory();
        }

        #region 注册

        /// <summary>
        /// 发送注册消息
        /// </summary>
        public event EventHandler<string> OnSendRegisterMessageHandler;
        /// <summary>
        /// 发送注册消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendRegisterMessage(RegisterParams param)
        {
            Helper.CloseTimeout(requestCacheId);
            eventHandlers.Send(new SendEventArg
            {
                Address = UdpServer,
                Data = new RegisterModel
                {
                    Name = param.ClientName,
                    GroupId = param.GroupId,
                    LocalIps = param.LocalIps,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort,

                }
            });

            requestCache = param;
            requestCacheId = Helper.SetTimeout(() =>
           {
               if (requestCache != null)
               {
                   Action<RegisterMessageFailModel> callback = requestCache.FailCallback;
                   requestCache = null;
                   callback.Invoke(new RegisterMessageFailModel
                   {
                       Type = RegisterMessageFailType.TIMEOUT,
                       Msg = "注册超时"
                   });
               }
           }, param.Timeout);

            OnSendRegisterMessageHandler?.Invoke(this, param.ClientName);
        }

        /// <summary>
        /// 发送Tcp注册消息
        /// </summary>
        public event EventHandler<string> OnSendTcpRegisterMessageHandler;
        /// <summary>
        /// 发送Tcp注册消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpRegisterMessage(long id, string clientName, string groupId = "", string mac = "", int localTcpport = 0, int localUdpPort = 0)
        {
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = TcpServer,
                Data = new RegisterModel
                {
                    Id = id,
                    Name = clientName,
                    GroupId = groupId,
                    Mac = mac,
                    LocalTcpPort = localTcpport,
                    LocalUdpPort = localUdpPort
                }
            });
            OnSendTcpRegisterMessageHandler?.Invoke(this, clientName);
        }

        /// <summary>
        /// 注册状态发生变化
        /// </summary>
        public event EventHandler<RegisterEventArg> OnSendRegisterStateChangeHandler;
        /// <summary>
        /// 发布注册状态变化消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendRegisterStateChange(RegisterEventArg arg)
        {
            OnSendRegisterStateChangeHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 注册Tcp状态发生变化
        /// </summary>
        public event EventHandler<RegisterTcpEventArg> OnSendRegisterTcpStateChangeHandler;
        /// <summary>
        /// 发布注册Tcp状态变化消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendRegisterTcpStateChange(RegisterTcpEventArg arg)
        {
            OnSendRegisterTcpStateChangeHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 注册消息
        /// </summary>
        public event EventHandler<OnRegisterResultEventArg> OnRegisterResultHandler;
        /// <summary>
        /// 注册消息
        /// </summary>
        /// <param name="arg"></param>
        public void OnRegisterResult(OnRegisterResultEventArg arg)
        {
            if (requestCache != null)
            {
                if (arg.Data.Code == 0)
                {
                    SendTcpRegisterMessage(arg.Data.Id, requestCache.ClientName, arg.Data.GroupId, arg.Data.Mac, arg.Data.LocalTcpPort, arg.Data.LocalUdpPort);
                    SendRegisterStateChange(new RegisterEventArg
                    {
                        ServerAddress = arg.Packet.SourcePoint,
                        ClientAddress = new IPEndPoint(IPAddress.Parse(arg.Data.Ip), arg.Data.Port),
                        State = true,
                        Id = arg.Data.Id,
                        ClientName = requestCache.ClientName
                    });
                }
                else
                {
                    Action<RegisterMessageFailModel> callback = requestCache.FailCallback;
                    requestCache = null;
                    callback.Invoke(new RegisterMessageFailModel
                    {
                        Type = RegisterMessageFailType.ERROR,
                        Msg = arg.Data.Msg
                    });
                }
                OnRegisterResultHandler?.Invoke(this, arg);
            }

        }

        /// <summary>
        /// 注册Tcp消息
        /// </summary>
        public event EventHandler<OnRegisterResultEventArg> OnRegisterTcpResultHandler;
        /// <summary>
        /// 注册Tcp消息
        /// </summary>
        /// <param name="arg"></param>
        public void OnRegisterTcpResult(OnRegisterResultEventArg arg)
        {
            if (requestCache != null)
            {
                SendRegisterTcpStateChange(new RegisterTcpEventArg
                {
                    State = true,
                    Id = arg.Data.Id,
                    ClientName = requestCache.ClientName,
                    Ip = arg.Data.Ip,
                });
                requestCache.Callback?.Invoke(arg.Data);
                requestCache = null;
                OnRegisterTcpResultHandler?.Invoke(this, arg);
            }

        }
        #endregion
    }

    #region 注册model
    public class OnRegisterResultEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public RegisterResultModel Data { get; set; }
    }

    public class RegisterEventArg : EventArgs
    {
        public IPEndPoint ServerAddress { get; set; }
        public IPEndPoint ClientAddress { get; set; }
        public long Id { get; set; }
        public string ClientName { get; set; }
        public bool State { get; set; }
    }
    public class RegisterTcpEventArg : EventArgs
    {
        public string Ip { get; set; }
        public long Id { get; set; }
        public string ClientName { get; set; }
        public bool State { get; set; }
    }
    #endregion

    public class RegisterParams
    {
        public string GroupId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string LocalIps { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public int Timeout { get; set; } = 15 * 1000;
        public int LocalUdpPort { get; set; } = 0;
        public int LocalTcpPort { get; set; } = 0;

        public Action<RegisterResultModel> Callback { get; set; } = null;
        public Action<RegisterMessageFailModel> FailCallback { get; set; } = null;
    }

    public class RegisterMessageCache
    {
        public long Time { get; set; } = 0;
        public int Timeout { get; set; } = 15 * 60 * 1000;
        public Action<RegisterResultModel> Callback { get; set; } = null;
        public Action<RegisterMessageFailModel> FailCallback { get; set; } = null;
    }

    public class RegisterMessageFailModel
    {
        public RegisterMessageFailType Type { get; set; } = RegisterMessageFailType.ERROR;
        public string Msg { get; set; } = string.Empty;
    }

    public enum RegisterMessageFailType
    {
        ERROR, TIMEOUT
    }

}
