﻿using client.service.config;
using client.service.events;
using client.service.serverPlugins.heart;
using common;
using common.extends;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.serverPlugins.register
{



    public class RegisterHelper
    {
        private readonly RegisterEventHandles registerEventHandles;
        private readonly HeartEventHandles heartEventHandles;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterState registerState;


        private long lastTime = 0;
        private long lastTcpTime = 0;
        private readonly int heartInterval = 5000;

        public RegisterHelper(RegisterEventHandles registerEventHandles, HeartEventHandles heartEventHandles,
            ITcpServer tcpServer, IUdpServer udpServer, Config config, RegisterState registerState)
        {
            this.registerEventHandles = registerEventHandles;
            this.heartEventHandles = heartEventHandles;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;

            //退出消息
            registerEventHandles.OnSendExitMessageHandler += (sender, e) =>
            {
                registerState.LocalInfo.IsConnecting = false;
                registerState.LocalInfo.Connected = false;
                registerState.LocalInfo.TcpConnected = false;
                ResetLastTime();
            };

            //给服务器发送心跳包
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (IsTimeout())
                    {
                        registerEventHandles.SendExitMessage().Wait();
                    }
                    if (registerState.LocalInfo.Connected && registerState.LocalInfo.TcpConnected)
                    {
                        heartEventHandles.SendHeartMessage(registerState.RemoteInfo.ConnectId, registerState.UdpAddress);
                        heartEventHandles.SendTcpHeartMessage(registerState.RemoteInfo.ConnectId, registerState.TcpSocket);
                    }
                    Thread.Sleep(heartInterval);
                }
            }, TaskCreationOptions.LongRunning);

            //收服务器的心跳包
            heartEventHandles.OnHeartEventHandler += (sender, e) =>
            {
                if (e.Data.SourceId == -1)
                {
                    if (e.Packet.ServerType == ServerType.UDP)
                    {
                        //Logger.Instance.Debug($"UDP收到服务器心跳~~~");
                        lastTime = Helper.GetTimeStamp();
                    }
                    else if (e.Packet.ServerType == ServerType.TCP)
                    {
                        //Logger.Instance.Debug($"TCP收到服务器心跳~~~");
                        lastTcpTime = Helper.GetTimeStamp();
                    }
                }
            };
        }

        public void AutoReg()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var result = Start().Result;
                    if (result.Data)
                    {
                        break;
                    }
                    Thread.Sleep(2000);
                }
            });
            Logger.Instance.Info("已自动注册...");
        }

        public async Task<CommonTaskResponseModel<bool>> Start()
        {
            var tcs = new TaskCompletionSource<CommonTaskResponseModel<bool>>();

            //不管三七二十一，先停止一波
            bool connecting = registerState.LocalInfo.IsConnecting;
            await registerEventHandles.SendExitMessage();
            if (connecting)
            {
                tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = "正在注册！" });
            }
            else
            {
                try
                {
                    registerState.LocalInfo.IsConnecting = true;

                    registerState.LocalInfo.Port = Helper.GetRandomPort();

                    //TCP 本地开始监听
                    registerState.LocalInfo.TcpPort = Helper.GetRandomPort(new List<int> { registerState.LocalInfo.Port });
                    tcpServer.Start(registerState.LocalInfo.TcpPort, config.Client.BindIp);

                    //TCP 连接服务器
                    registerState.TcpSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    registerState.TcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    registerState.TcpSocket.Bind(new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort));
                    registerState.TcpSocket.Connect(new IPEndPoint(IPAddress.Parse(config.Server.Ip), config.Server.TcpPort));
                    registerState.LocalInfo.LocalIp = IPEndPoint.Parse(registerState.TcpSocket.LocalEndPoint.ToString()).Address.ToString();
                    tcpServer.BindReceive(registerState.TcpSocket, (code) =>
                    {
                        if (code == SocketError.ConnectionAborted)
                        {
                            AutoReg();
                        }
                    });

                    //上报mac
                    string mac = string.Empty;
                    if (config.Client.UseMac)
                    {
                        registerState.LocalInfo.Mac = mac = Helper.GetMacAddress(IPEndPoint.Parse(registerState.TcpSocket.LocalEndPoint.ToString()).Address.ToString());
                    }
                    //UDP 开始监听
                    udpServer.Start(registerState.LocalInfo.Port, config.Client.BindIp);
                    registerState.UdpAddress = new IPEndPoint(IPAddress.Parse(config.Server.Ip), config.Server.Port);


                    //注册
                    RegisterResultModel result = await registerEventHandles.SendRegisterMessage(new RegisterParams
                    {
                        ClientName = config.Client.Name,
                        GroupId = config.Client.GroupId,
                        LocalUdpPort = registerState.LocalInfo.Port,
                        LocalTcpPort = registerState.LocalInfo.TcpPort,
                        Mac = mac,
                        LocalIps = IPEndPoint.Parse(registerState.TcpSocket.LocalEndPoint.ToString()).Address.ToString(),
                        Timeout = 5 * 1000
                    });
                    if (result.Code == 0)
                    {
                        registerState.LocalInfo.IsConnecting = false;
                        config.Client.GroupId = result.GroupId;
                        registerState.RemoteInfo.Ip = result.Ip;
                        registerState.RemoteInfo.ConnectId = result.Id;
                        registerState.LocalInfo.Connected = true;
                        registerState.LocalInfo.TcpConnected = true;
                        registerState.RemoteInfo.TcpPort = result.TcpPort;
                        tcs.SetResult(new CommonTaskResponseModel<bool> { Data = true, ErrorMsg = string.Empty });
                    }
                    else
                    {
                        registerState.LocalInfo.IsConnecting = false;
                        tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = result.Msg });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex.Message);
                    tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = ex.Message });
                    registerState.LocalInfo.IsConnecting = false;
                    await registerEventHandles.SendExitMessage();
                }
            }

            return await tcs.Task;
        }

        private void ResetLastTime()
        {
            lastTime = 0;
            lastTcpTime = 0;
        }
        private bool IsTimeout()
        {
            long time = Helper.GetTimeStamp();
            return (lastTime > 0 && time - lastTime > 20000) || (lastTcpTime > 0 && time - lastTcpTime > 20000);
        }
    }

    public class RegisterState
    {
        public Socket TcpSocket { get; set; }
        public IPEndPoint UdpAddress { get; set; }
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
    }

    public class RemoteInfo
    {
        /// <summary>
        /// 客户端在远程的ip
        /// </summary>
        public string Ip { get; set; } = string.Empty;
        /// <summary>
        /// 客户端在远程的TCP端口
        /// </summary>
        public int TcpPort { get; set; } = 0;
        /// <summary>
        /// 客户端连接ID
        /// </summary>
        public long ConnectId { get; set; } = 0;
    }

    public class LocalInfo
    {
        /// <summary>
        /// 外网距离
        /// </summary>
        public int RouteLevel { get; set; } = 0;
        /// <summary>
        /// 本地mac地址
        /// </summary>
        public string Mac { get; set; } = string.Empty;
        /// <summary>
        /// 本地UDP端口
        /// </summary>
        public int Port { get; set; } = 0;
        /// <summary>
        /// 本地TCP端口
        /// </summary>
        public int TcpPort { get; set; } = 0;

        public string LocalIp { get; set; } = string.Empty;

        /// <summary>
        /// 是否正在连接服务器
        /// </summary>
        public bool IsConnecting { get; set; } = false;
        /// <summary>
        /// UDP是否已连接服务器
        /// </summary>
        public bool Connected { get; set; } = false;
        /// <summary>
        /// TCP是否已连接服务器
        /// </summary>
        public bool TcpConnected { get; set; } = false;
    }
}
