using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
using server;
using server.model;
using server.models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static server.model.RegisterResultModel;

namespace client.service.plugins.serverPlugins.register
{
    public class RegisterMessageHelper
    {
        private readonly IServerRequest serverRequest;
        private readonly RegisterState registerState;

        public RegisterMessageHelper(IServerRequest serverRequest, RegisterState registerState)
        {
            this.serverRequest = serverRequest;
            this.registerState = registerState;
        }

        /// <summary>
        /// 发送退出消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task SendExitMessage()
        {
            MessageRequestResponeWrap res = await serverRequest.SendReply(new SendArg<ExitModel>
            {
                Connection = registerState.TcpConnection,
                Data = new ExitModel { },
                Path = "exit/Execute"
            });
        }

        /// <summary>
        /// 发送注册消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task<RegisterResult> SendRegisterMessage(RegisterParams param)
        {
            MessageRequestResponeWrap result = await serverRequest.SendReply(new SendArg<RegisterModel>
            {
                Connection = registerState.UdpConnection,
                Path = "register/Execute",
                Data = new RegisterModel
                {
                    Name = param.ClientName,
                    GroupId = param.GroupId,
                    LocalIps = param.LocalIps,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort,

                },
                Timeout = param.Timeout,
            });
            if (result.Code != MessageResponeCode.OK)
            {
                return new RegisterResult { NetState = result };
            }

            RegisterResultModel res = result.Data.DeBytes<RegisterResultModel>();
            if (res.Code != RegisterResultCodes.OK)
            {
                return new RegisterResult { NetState = result, Data = res };
            }

            MessageRequestResponeWrap tcpResult = await serverRequest.SendReply(new SendArg<RegisterModel>
            {
                Connection = registerState.TcpConnection,
                Path = "register/Execute",
                Data = new RegisterModel
                {
                    Id = res.Id,
                    Name = param.ClientName,
                    GroupId = res.GroupId,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort
                },
                Timeout = param.Timeout,
            });

            if (tcpResult.Code != MessageResponeCode.OK)
            {
                return new RegisterResult { NetState = tcpResult };
            }

            RegisterResultModel tcpRes = tcpResult.Data.DeBytes<RegisterResultModel>();
            return new RegisterResult { NetState = tcpResult, Data = tcpRes };
        }

        /// <summary>
        /// 发送通知消息，通知服务器，告诉所有客户端，有新客户端上线了，发送一下客户端列表
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SendNotifyMessage()
        {
            return await serverRequest.SendOnly(new SendArg<RegisterNotifyModel>
            {
                Connection = registerState.TcpConnection,
                Data = new RegisterNotifyModel { },
                Path = "register/notify"
            });
        }
    }

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

    public class RegisterResult
    {
        public MessageRequestResponeWrap NetState { get; set; }
        public RegisterResultModel Data { get; set; }
    }
}
