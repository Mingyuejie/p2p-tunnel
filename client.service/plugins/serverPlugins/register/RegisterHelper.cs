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
        private readonly RegisterMessageHelper registerEventHandles;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterState registerState;
        private readonly HeartMessageHelper heartEventHandles;
        private readonly ServerPluginHelper serverPluginHelper;

        private long lastTime = 0;
        private long lastTcpTime = 0;
        private readonly int heartInterval = 5000;

        public RegisterHelper(
            RegisterMessageHelper registerEventHandles, HeartMessageHelper heartEventHandles,
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
            registerEventHandles.OnRegisterStateChange.Sub((e) =>
            {
                if (e.State == false)
                {
                    registerState.Offline();
                    ResetLastTime();
                }
            });

            Heart();
        }

        public void AutoReg()
        {
            if (config.Client.AutoReg)
            {
                if (registerState.LocalInfo.Connected)
                {
                    registerEventHandles.SendExitMessage().Wait();
                }

                Task.Run(async () =>
                {
                    CommonTaskResponseModel<bool> result = null;
                    while (result == null || result.Data == false)
                    {
                        result = await Start();
                        if (result.Data == true)
                        {
                            break;
                        }
                        await Task.Delay(500);
                    }
                });
                Logger.Instance.Info("已自动注册...");
            }
        }

        public async Task<CommonTaskResponseModel<bool>> Start()
        {
            TaskCompletionSource<CommonTaskResponseModel<bool>> tcs = new TaskCompletionSource<CommonTaskResponseModel<bool>>();

            await registerEventHandles.SendExitMessage();

            try
            {
                IPAddress serverAddress = Helper.GetDomainIp(config.Server.Ip);
                registerState.LocalInfo.IsConnecting = true;
                registerState.LocalInfo.Port = Helper.GetRandomPort();
                registerState.LocalInfo.TcpPort = Helper.GetRandomPort(new List<int> { registerState.LocalInfo.Port });
                registerState.LocalInfo.Mac = string.Empty;
                registerState.UdpAddress = new IPEndPoint(serverAddress, config.Server.Port);

                //TCP 本地开始监听
                tcpServer.Start(registerState.LocalInfo.TcpPort, config.Client.BindIp);
                //UDP 开始监听
                udpServer.Start(registerState.LocalInfo.Port, config.Client.BindIp);

                //TCP 连接服务器
                registerState.TcpSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                registerState.TcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                registerState.TcpSocket.Bind(new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort));
                registerState.TcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
                registerState.LocalInfo.LocalIp = IPEndPoint.Parse(registerState.TcpSocket.LocalEndPoint.ToString()).Address.ToString();
                if (config.Client.UseMac)
                {
                    registerState.LocalInfo.Mac = Helper.GetMacAddress(IPEndPoint.Parse(registerState.TcpSocket.LocalEndPoint.ToString()).Address.ToString());
                }
                tcpServer.BindReceive(registerState.TcpSocket);
                //注册
                RegisterResultModel result = await registerEventHandles.SendRegisterMessage(new RegisterParams
                {
                    ClientName = config.Client.Name,
                    GroupId = config.Client.GroupId,
                    LocalUdpPort = registerState.LocalInfo.Port,
                    LocalTcpPort = registerState.LocalInfo.TcpPort,
                    Mac = registerState.LocalInfo.Mac,
                    LocalIps = string.Join(",", new List<string> {
                            "127.0.0.1",
                            IPEndPoint.Parse(registerState.TcpSocket.LocalEndPoint.ToString()).Address.ToString()
                        }),
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
                    await registerEventHandles.SendExitMessage();
                    tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = result.Msg });
                }
            }
            catch (Exception ex)
            {
                await registerEventHandles.SendExitMessage();
                tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = ex.Message });
                Logger.Instance.Error(ex + "");
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
            return ((lastTime == 0 || time - lastTime > 5000) || (lastTcpTime == 0 || time - lastTcpTime > 5000));
        }

        private void Heart()
        {
            //给服务器发送心跳包
            _ = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (IsTimeout())
                    {
                        AutoReg();
                    }
                    else if (IsNeedHeart())
                    {
                        if (registerState.LocalInfo.Connected)
                        {
                            heartEventHandles.SendHeartMessage(registerState.RemoteInfo.ConnectId, registerState.UdpAddress);
                        }
                        if (registerState.LocalInfo.TcpConnected)
                        {
                            heartEventHandles.SendTcpHeartMessage(registerState.RemoteInfo.ConnectId, registerState.TcpSocket);
                        }
                    }
                    await Task.Delay(heartInterval);
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
