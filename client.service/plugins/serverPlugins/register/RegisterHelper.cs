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
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.register
{
    public class RegisterHelper
    {
        private readonly RegisterMessageHelper registerMessageHelper;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterState registerState;
        private readonly HeartMessageHelper heartMessageHelper;

        public RegisterHelper(
            RegisterMessageHelper registerMessageHelper, HeartMessageHelper heartMessageHelper,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterState registerState
        )
        {
            this.registerMessageHelper = registerMessageHelper;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.heartMessageHelper = heartMessageHelper;

            Heart();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => _ = Exit();
            Console.CancelKeyPress += (s, e) => _ = Exit();
        }

        public async Task AutoReg()
        {
            if (config.Client.AutoReg)
            {
                Logger.Instance.Info("开始自动注册");
                while (true)
                {
                    CommonTaskResponseModel<bool> result = await Register();
                    if (result.Data == true)
                    {
                        break;
                    }
                    else
                    {
                        Logger.Instance.Error(result.ErrorMsg);
                    }
                    await Task.Delay(1000);
                }
                Logger.Instance.Warning("已自动注册");
            }
        }

        public async Task Exit()
        {
            await registerMessageHelper.SendExitMessage();
            udpServer.Stop();
            tcpServer.Stop();
            registerState.Offline();
            Helper.FlushMemory();
        }

        public async Task<CommonTaskResponseModel<bool>> Register()
        {
            TaskCompletionSource<CommonTaskResponseModel<bool>> tcs = new TaskCompletionSource<CommonTaskResponseModel<bool>>();
            await Exit();

            try
            {
                IPAddress serverAddress = Helper.GetDomainIp(config.Server.Ip);
                registerState.LocalInfo.IsConnecting = true;
                registerState.LocalInfo.Port = Helper.GetRandomPort();
                registerState.LocalInfo.TcpPort = Helper.GetRandomPort(new List<int> { registerState.LocalInfo.Port });
                registerState.LocalInfo.Mac = string.Empty;

                //UDP 开始监听
                udpServer.Start(registerState.LocalInfo.Port, config.Client.BindIp);
                registerState.UdpConnection = udpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.Port));

                //TCP 本地开始监听
                tcpServer.SetBufferSize(config.Client.TcpBufferSize);
                tcpServer.Start(registerState.LocalInfo.TcpPort, config.Client.BindIp);
                //TCP 连接服务器
                IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort);
                Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                tcpSocket.KeepAlive();
                tcpSocket.ReuseBind(bindEndpoint);
                tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
                registerState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address.ToString();
                if (config.Client.UseMac)
                {
                    registerState.LocalInfo.Mac = Helper.GetMacAddress(registerState.LocalInfo.LocalIp);
                }
                tcpServer.BindReceive(tcpSocket, async (socketError) =>
                {
                    await AutoReg();
                }, config.Client.TcpBufferSize);
                registerState.TcpConnection = tcpServer.CreateConnection(tcpSocket);


                //注册
                RegisterResult result = await registerMessageHelper.SendRegisterMessage(new RegisterParams
                {
                    ClientName = config.Client.Name,
                    GroupId = config.Client.GroupId,
                    LocalUdpPort = registerState.LocalInfo.Port,
                    LocalTcpPort = registerState.LocalInfo.TcpPort,
                    Mac = registerState.LocalInfo.Mac,
                    LocalIps = string.Join(Helper.SeparatorString, new List<string> { config.Client.LoopbackIp.ToString(), registerState.LocalInfo.LocalIp }),
                    Timeout = 5 * 1000
                });
                if (result.NetState.Code != MessageResponeCode.OK)
                {
                    tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = $"注册失败，网络问题:{result.NetState.Code.GetDesc((byte)result.NetState.Code)}" });
                }
                else
                {
                    if (result.Data.Code == RegisterResultModel.RegisterResultCodes.OK)
                    {
                        config.Client.GroupId = result.Data.GroupId;
                        registerState.Online(result.Data.Id, result.Data.Ip, result.Data.TcpPort);
                        bool notifyRes = await registerMessageHelper.SendNotifyMessage();
                        if (notifyRes)
                        {
                            Logger.Instance.Warning("已通知上线信息");
                        }
                        else
                        {
                            Logger.Instance.Error("通知上线信息失败");
                        }

                        tcs.SetResult(new CommonTaskResponseModel<bool> { Data = true, ErrorMsg = string.Empty });
                    }
                    else
                    {
                        await Exit();
                        tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = $"注册失败:{result.Data.Code.GetDesc((byte)result.Data.Code)}" });
                    }
                }
            }
            catch (Exception ex)
            {
                await Exit();
                tcs.SetResult(new CommonTaskResponseModel<bool> { Data = false, ErrorMsg = ex.Message });
                Logger.Instance.Error(ex);
            }

            return await tcs.Task;
        }

        private void Heart()
        {
            //给服务器发送心跳包
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (registerState.UdpConnection != null)
                    {
                        long time = Helper.GetTimeStamp();
                        if (registerState.UdpConnection.IsTimeout(time))
                        {
                            await AutoReg();
                        }
                        else if (registerState.UdpConnection.IsNeedHeart(time))
                        {
                            await heartMessageHelper.SendHeartMessage(registerState.UdpConnection);
                        }
                    }
                    await Task.Delay(5000);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
