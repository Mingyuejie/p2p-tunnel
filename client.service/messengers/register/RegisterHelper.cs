using client.messengers.register;
using client.service.messengers.heart;
using common;
using common.extends;
using server;
using server.model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.messengers.register
{
    public class RegisterHelper
    {
        private readonly RegisterMessengerSender registerMessageHelper;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterStateInfo registerState;
        private readonly HeartMessengerSender  heartMessengerSender;

        public RegisterHelper(
            RegisterMessengerSender registerMessageHelper, HeartMessengerSender heartMessengerSender,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterStateInfo registerState
        )
        {
            this.registerMessageHelper = registerMessageHelper;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.heartMessengerSender = heartMessengerSender;

            Heart();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => _ = Exit();
            Console.CancelKeyPress += (s, e) => _ = Exit();
        }

        internal async Task AutoReg()
        {
            if (config.Client.AutoReg)
            {
                Logger.Instance.Info("开始自动注册");
                while (true)
                {
                    CommonTaskResponseInfo<bool> result = await Register();
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

        internal async Task Exit()
        {
            await registerMessageHelper.Exit();
            udpServer.Stop();
            tcpServer.Stop();
            registerState.Offline();
            GCHelper.FlushMemory();
        }

        internal async Task<CommonTaskResponseInfo<bool>> Register()
        {
            TaskCompletionSource<CommonTaskResponseInfo<bool>> tcs = new TaskCompletionSource<CommonTaskResponseInfo<bool>>();
            await Exit();

            try
            {
                IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                registerState.LocalInfo.IsConnecting = true;
                registerState.LocalInfo.UdpPort = NetworkHelper.GetRandomPort();
                registerState.LocalInfo.TcpPort = NetworkHelper.GetRandomPort(new List<int> { registerState.LocalInfo.UdpPort });
                registerState.LocalInfo.Mac = string.Empty;

                //UDP 开始监听
                udpServer.Start(registerState.LocalInfo.UdpPort, config.Client.BindIp);
                registerState.UdpConnection = udpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));

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
                    registerState.LocalInfo.Mac = NetworkHelper.GetMacAddress(registerState.LocalInfo.LocalIp);
                }
                registerState.TcpConnection = tcpServer.BindReceive(tcpSocket, async (socketError) =>
                {
                    await AutoReg();
                }, config.Client.TcpBufferSize);

                //注册
                RegisterResult result = await registerMessageHelper.Register(new RegisterParams
                {
                    ClientName = config.Client.Name,
                    GroupId = config.Client.GroupId,
                    LocalUdpPort = registerState.LocalInfo.UdpPort,
                    LocalTcpPort = registerState.LocalInfo.TcpPort,
                    Mac = registerState.LocalInfo.Mac,
                    LocalIps = string.Join(Helper.SeparatorString, new List<string> { config.Client.LoopbackIp.ToString(), registerState.LocalInfo.LocalIp }),
                    Timeout = 5 * 1000
                });
                if (result.NetState.Code != MessageResponeCodes.OK)
                {
                    tcs.SetResult(new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = $"注册失败，网络问题:{result.NetState.Code.GetDesc((byte)result.NetState.Code)}" });
                }
                else
                {
                    if (result.Data.Code == RegisterResultInfo.RegisterResultInfoCodes.OK)
                    {
                        config.Client.GroupId = result.Data.GroupId;
                        registerState.Online(result.Data.Id, result.Data.Ip, result.Data.TcpPort);
                        if (await registerMessageHelper.Notify())
                        {
                            Logger.Instance.Warning("已通知上线信息");
                        }
                        else
                        {
                            Logger.Instance.Error("通知上线信息失败");
                        }

                        tcs.SetResult(new CommonTaskResponseInfo<bool> { Data = true, ErrorMsg = string.Empty });
                    }
                    else
                    {
                        await Exit();
                        tcs.SetResult(new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = $"注册失败:{result.Data.Code.GetDesc((byte)result.Data.Code)}" });
                    }
                }
            }
            catch (Exception ex)
            {
                await Exit();
                tcs.SetResult(new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = ex.Message });
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
                        long time = DateTimeHelper.GetTimeStamp();
                        if (registerState.UdpConnection.IsTimeout(time))
                        {
                            await AutoReg();
                        }
                        else if (registerState.UdpConnection.IsNeedHeart(time))
                        {
                            await heartMessengerSender.Heart(registerState.UdpConnection);
                        }
                    }
                    await Task.Delay(5000);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
