using client.service.events;
using common;
using common.extends;
using server;
using server.model;
using server.models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.serverPlugins.register
{
    public class RegisterEventHandles
    {
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

            AppDomain.CurrentDomain.ProcessExit += (s, e) => _ = SendExitMessage();
            Console.CancelKeyPress += (s, e) => _ = SendExitMessage();
        }

        private IPEndPoint UdpServer => registerState.UdpAddress;
        private Socket TcpServer => registerState.TcpSocket;
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        public int ClientTcpPort => registerState.RemoteInfo.TcpPort;
        public int RouteLevel => registerState.LocalInfo.RouteLevel;

        /// <summary>
        /// 发送退出消息
        /// </summary>
        public event EventHandler<SendEventArg<ExitModel>> OnSendExitMessageHandler;
        /// <summary>
        /// 发送退出消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task SendExitMessage()
        {
            SendEventArg<ExitModel> arg = new()
            {
                Address = UdpServer,
                Data = new ExitModel
                {
                    Id = ConnectId,
                }
            };

            if (UdpServer != null)
            {
                await eventHandlers.SendReply(new SendEventArg<ExitModel>
                {
                    Address = arg.Address,
                    Data = arg.Data,
                    Path = "exit/excute"
                });
                udpServer.Stop();
                tcpServer.Stop();

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
        public async Task<RegisterResultModel> SendRegisterMessage(RegisterParams param)
        {
            Logger.Instance.Debug("1");
            OnSendRegisterMessageHandler?.Invoke(this, param.ClientName);
            ServerMessageResponeWrap result = await eventHandlers.SendReply(new SendEventArg<RegisterModel>
            {
                Address = UdpServer,
                Path = "register/excute",
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
            Logger.Instance.Debug("2");
            if (result.Code != ServerMessageResponeCodes.OK)
            {
                return new RegisterResultModel { Code = -1, Msg = result.ErrorMsg };
            }
            Logger.Instance.Debug("3");

            var res = result.Data.DeBytes<RegisterResultModel>();
            var tcpResult = await eventHandlers.SendReplyTcp(new SendTcpEventArg<RegisterModel>
            {
                Socket = TcpServer,
                Path = "register/excute",
                Data = new RegisterModel
                {
                    Id = res.Id,
                    Name = param.ClientName,
                    GroupId = res.GroupId,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort
                }
            });
            Logger.Instance.Debug("4");
            if (tcpResult.Code != ServerMessageResponeCodes.OK)
            {
                return new RegisterResultModel { Code = -1, Msg = tcpResult.ErrorMsg };
            }
            SendRegisterTcpStateChange(new RegisterTcpEventArg
            {
                State = true,
                Id = res.Id,
                ClientName = param.ClientName,
                Ip = res.Ip,
            });
            Logger.Instance.Debug("5");
            return tcpResult.Data.DeBytes<RegisterResultModel>();

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
        #endregion
    }

    #region 注册model

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

    }
}
