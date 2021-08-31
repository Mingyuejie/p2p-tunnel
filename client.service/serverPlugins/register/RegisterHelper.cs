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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.serverPlugins.register
{
    public class RegisterHelper
    {
        private static readonly Lazy<RegisterHelper> lazy = new(() => new RegisterHelper());
        public static RegisterHelper Instance => lazy.Value;

        private long lastTime = 0;
        private long lastTcpTime = 0;
        private readonly int heartInterval = 5000;
        public event EventHandler<bool> OnRegisterChange;

        private RegisterHelper()
        {
            //退出消息
            RegisterEventHandles.Instance.OnSendExitMessageHandler += (sender, e) =>
            {
                AppShareData.Instance.IsConnecting = false;
                AppShareData.Instance.Connected = false;
                AppShareData.Instance.TcpConnected = false;
                ResetLastTime();
                OnRegisterChange?.Invoke(this, false);
            };

            //给服务器发送心跳包
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (IsTimeout())
                    {
                        RegisterEventHandles.Instance.SendExitMessage();
                    }
                    if (AppShareData.Instance.Connected && AppShareData.Instance.TcpConnected)
                    {
                        HeartEventHandles.Instance.SendHeartMessage();
                        HeartEventHandles.Instance.SendTcpHeartMessage();
                    }
                    Thread.Sleep(heartInterval);
                }
            }, TaskCreationOptions.LongRunning);

            //收服务器的心跳包
            HeartEventHandles.Instance.OnHeartEventHandler += (sender, e) =>
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
            };
        }

        public void AutoReg()
        {
            Task.Run(() =>
            {
                Start((msg) =>
                {
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        Helper.SetTimeout(() =>
                        {
                            AutoReg();
                        }, 2000);
                    }
                });
            });
        }

        public void Start(Action<string> callback = null)
        {
            //不管三七二十一，先停止一波
            bool connecting = AppShareData.Instance.IsConnecting;
            RegisterEventHandles.Instance.SendExitMessage();
            if (connecting)
            {
                callback?.Invoke("正在注册！");
            }
            else
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        AppShareData.Instance.IsConnecting = true;
                        ResetLastTime();

                        AppShareData.Instance.ClientPort = Helper.GetRandomPort();

                        AppShareData.Instance.ClientTcpPort = Helper.GetRandomPort(new List<int> { AppShareData.Instance.ClientPort });
                        TCPServer.Instance.Start(AppShareData.Instance.ClientTcpPort, AppShareData.Instance.LocalIp);

                        //TCP 开始监听
                        Socket serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(AppShareData.Instance.ServerIp), AppShareData.Instance.ServerTcpPort);
                        serverSocket.Bind(new IPEndPoint(AppShareData.Instance.LocalIp, AppShareData.Instance.ClientTcpPort));
                        AppShareData.Instance.TcpServer = serverSocket;
                        serverSocket.Connect(remoteEndPoint);

                        TCPServer.Instance.BindReceive(serverSocket);

                        string mac = string.Empty;
                        if (AppShareData.Instance.UseMac)
                        {
                            AppShareData.Instance.Mac = mac = Helper.GetMacAddress(IPEndPoint.Parse(serverSocket.LocalEndPoint.ToString()).Address.ToString());
                        }


                        //UDP 开始监听
                        UDPServer.Instance.Start(AppShareData.Instance.ClientPort, AppShareData.Instance.LocalIp);
                        AppShareData.Instance.UdpServer = new IPEndPoint(IPAddress.Parse(AppShareData.Instance.ServerIp), AppShareData.Instance.ServerPort);
                        RegisterEventHandles.Instance.SendRegisterMessage(new RegisterParams
                        {
                            ClientName = AppShareData.Instance.ClientName,
                            GroupId = AppShareData.Instance.GroupId,
                            LocalTcpPort = 0,//AppShareData.Instance.ClientTcpPort,
                            Mac = mac,
                            LocalIps = IPEndPoint.Parse(serverSocket.LocalEndPoint.ToString()).Address.ToString(),
                            Timeout = 5 * 1000,
                            Callback = (result) =>
                            {
                                AppShareData.Instance.IsConnecting = false;
                                AppShareData.Instance.GroupId = result.GroupId;
                                AppShareData.Instance.Ip = result.Ip;
                                AppShareData.Instance.ConnectId = result.Id;
                                AppShareData.Instance.Connected = true;
                                AppShareData.Instance.TcpConnected = true;
                                AppShareData.Instance.ClientTcpPort2 = result.TcpPort;

                                OnRegisterChange?.Invoke(this, true);
                                callback?.Invoke(string.Empty);
                            },
                            FailCallback = (fail) =>
                            {
                                AppShareData.Instance.IsConnecting = false;
                                OnRegisterChange?.Invoke(this, false);
                                callback?.Invoke(fail.Msg);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex.Message);
                        callback?.Invoke(ex.Message);
                        AppShareData.Instance.IsConnecting = false;
                        RegisterEventHandles.Instance.SendExitMessage();
                    }
                });
            }
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
}
