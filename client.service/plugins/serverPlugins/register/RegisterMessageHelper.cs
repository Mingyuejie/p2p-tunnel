﻿using client.plugins.serverPlugins;
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

namespace client.service.plugins.serverPlugins.register
{
    public class RegisterMessageHelper
    {
        private readonly IServerRequest serverRequest;
        private readonly RegisterState registerState;
        private readonly IUdpServer udpServer;
        private readonly ITcpServer tcpServer;

        public RegisterMessageHelper(IServerRequest serverRequest, RegisterState registerState, IUdpServer udpServer, ITcpServer tcpServer)
        {
            this.serverRequest = serverRequest;
            this.registerState = registerState;
            this.udpServer = udpServer;
            this.tcpServer = tcpServer;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => _ = SendExitMessage();
            Console.CancelKeyPress += (s, e) => _ = SendExitMessage();
        }

        /// <summary>
        /// 注册Tcp状态发生变化
        /// </summary>
        public SimplePushSubHandler<RegisterEventArg> OnRegisterStateChange { get; } = new SimplePushSubHandler<RegisterEventArg>();

        /// <summary>
        /// 发送退出消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task SendExitMessage()
        {
            await serverRequest.SendReplyTcp(new SendTcpEventArg<ExitModel>
            {
                Socket = registerState.TcpSocket,
                Data = new ExitModel
                {
                    Id = registerState.RemoteInfo.ConnectId,
                },
                Path = "exit/excute"
            });

            udpServer.Stop();
            tcpServer.Stop();

            OnRegisterStateChange.Push(new RegisterEventArg
            {
                State = false
            });

            Helper.FlushMemory();
        }

        /// <summary>
        /// 发送注册消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task<RegisterResultModel> SendRegisterMessage(RegisterParams param)
        {
            ServerMessageResponeWrap result = await serverRequest.SendReply(new SendEventArg<RegisterModel>
            {
                Address = registerState.UdpAddress,
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
            if (result.Code != ServerMessageResponeCodes.OK)
            {
                return new RegisterResultModel { Code = -1, Msg = result.ErrorMsg };
            }

            RegisterResultModel res = result.Data.DeBytes<RegisterResultModel>();

            ServerMessageResponeWrap tcpResult = await serverRequest.SendReplyTcp(new SendTcpEventArg<RegisterModel>
            {
                Socket = registerState.TcpSocket,
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
            if (tcpResult.Code != ServerMessageResponeCodes.OK)
            {
                return new RegisterResultModel { Code = -1, Msg = tcpResult.ErrorMsg };
            }
            OnRegisterStateChange.Push(new RegisterEventArg
            {
                State = true,
                Id = res.Id,
                ClientName = param.ClientName,
                Ip = res.Ip,
            });
            return tcpResult.Data.DeBytes<RegisterResultModel>();

        }

    }

    #region 注册model

    public class RegisterEventArg : EventArgs
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