using client.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.heart;
using common;
using common.extends;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.register
{
    public class RegisterHelper
    {
        private readonly RegisterEventHandles registerEventHandles;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterState registerState;
        private readonly HeartEventHandles heartEventHandles;
        private readonly ServerPluginHelper serverPluginHelper;

        private long lastTime = 0;
        private long lastTcpTime = 0;
        private readonly int heartInterval = 5000;

        public RegisterHelper(
            RegisterEventHandles registerEventHandles, HeartEventHandles heartEventHandles,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterState registerState,
            ServerPluginHelper serverPluginHelper
        )
        {
            this.registerEventHandles = registerEventHandles;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.heartEventHandles = heartEventHandles;
            this.serverPluginHelper = serverPluginHelper;

            //退出消息
            registerEventHandles.OnExitMessage.Sub((e) =>
            {
                registerState.Offline();
                ResetLastTime();
            });

            Heart();
        }

        public void AutoReg()
        {
            Task.Run(() =>
            {
                while (!registerState.LocalInfo.Connected)
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
                    IPAddress serverAddress = Helper.GetDomainIp(config.Server.Ip);

                    //TCP 本地开始监听
                    registerState.LocalInfo.TcpPort = Helper.GetRandomPort(new List<int> { registerState.LocalInfo.Port });
                    tcpServer.Start(registerState.LocalInfo.TcpPort, config.Client.BindIp);

                    //TCP 连接服务器
                    registerState.TcpSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    registerState.TcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    registerState.TcpSocket.Bind(new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort));
                    registerState.TcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
                    registerState.LocalInfo.LocalIp = IPEndPoint.Parse(registerState.TcpSocket.LocalEndPoint.ToString()).Address.ToString();
                    tcpServer.BindReceive(registerState.TcpSocket, (code) =>
                    {
                        if(code == SocketError.ConnectionReset)
                        {
                            registerEventHandles.SendExitMessage().Wait();
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
                    registerState.UdpAddress = new IPEndPoint(serverAddress, config.Server.Port);


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
                        config.Client.GroupId = result.GroupId;
                        registerState.Online(result.Id, result.Ip, result.TcpPort);
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
                    Logger.Instance.Error(ex + "");
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

        private bool IsNeedHeart()
        {
            long time = Helper.GetTimeStamp();
            return (lastTime == 0 || time - lastTime > 5000) || (lastTcpTime == 0 || time - lastTcpTime > 5000);
        }


        private void OnData(OnDataParam param)
        {
            if (param.Address == registerState.TcpAddressId)
            {
                lastTcpTime = param.Time;
            }
            else if (param.Address == registerState.UdpAddressId)
            {
                lastTime = param.Time;
            }
        }
        private void Heart()
        {

            serverPluginHelper.OnInputData.Sub(OnData);
            serverPluginHelper.OnSendData.Sub(OnData);

            //给服务器发送心跳包
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (IsTimeout())
                    {
                        registerEventHandles.SendExitMessage().Wait();

                        if (config.Client.AutoReg)
                        {
                            AutoReg();
                        }
                    }
                    if (registerState.LocalInfo.Connected && registerState.LocalInfo.TcpConnected && IsNeedHeart())
                    {
                        heartEventHandles.SendHeartMessage(registerState.RemoteInfo.ConnectId, registerState.UdpAddress);
                        heartEventHandles.SendTcpHeartMessage(registerState.RemoteInfo.ConnectId, registerState.TcpSocket);
                    }
                    Thread.Sleep(heartInterval);
                }
            }, TaskCreationOptions.LongRunning);

            //收服务器的心跳包
            heartEventHandles.OnHeart.Sub((e) =>
            {
                if (e.Data.SourceId == -1)
                {
                    if (e.Packet.ServerType == ServerType.UDP)
                    {
                        lastTime = Helper.GetTimeStamp();
                    }
                    else if (e.Packet.ServerType == ServerType.TCP)
                    {
                        lastTcpTime = Helper.GetTimeStamp();
                    }
                }
            });
        }
    }
}
