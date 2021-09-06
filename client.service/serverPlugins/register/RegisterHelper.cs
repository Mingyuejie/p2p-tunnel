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
                AppShareData.Instance.LocalInfo.IsConnecting = false;
                AppShareData.Instance.LocalInfo.Connected = false;
                AppShareData.Instance.LocalInfo.TcpConnected = false;
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
                    if (AppShareData.Instance.LocalInfo.Connected && AppShareData.Instance.LocalInfo.TcpConnected)
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
                        Logger.Instance.Info($"收到服务器心跳~~~");
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
            bool connecting = AppShareData.Instance.LocalInfo.IsConnecting;
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
                        AppShareData.Instance.LocalInfo.IsConnecting = true;

                        AppShareData.Instance.LocalInfo.Port = Helper.GetRandomPort();

                        //TCP 本地开始监听
                        AppShareData.Instance.LocalInfo.TcpPort = Helper.GetRandomPort(new List<int> { AppShareData.Instance.LocalInfo.Port });
                        TCPServer.Instance.Start(AppShareData.Instance.LocalInfo.TcpPort, AppShareData.Instance.LocalInfo.LocalIp);

                        //TCP 连接服务器
                        Socket serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(AppShareData.Instance.ServerConfig.Ip), AppShareData.Instance.ServerConfig.TcpPort);
                        serverSocket.Bind(new IPEndPoint(AppShareData.Instance.LocalInfo.LocalIp, AppShareData.Instance.LocalInfo.TcpPort));
                        AppShareData.Instance.TcpServer = serverSocket;
                        serverSocket.Connect(remoteEndPoint);
                        TCPServer.Instance.BindReceive(serverSocket,(code)=> { 
                            if(code == SocketError.ConnectionAborted)
                            {
                                AutoReg();
                            }
                        });

                        //上报mac
                        string mac = string.Empty;
                        if (AppShareData.Instance.ClientConfig.UseMac)
                        {
                            AppShareData.Instance.LocalInfo.Mac = mac = Helper.GetMacAddress(IPEndPoint.Parse(serverSocket.LocalEndPoint.ToString()).Address.ToString());
                        }

                        //UDP 开始监听
                        UDPServer.Instance.Start(AppShareData.Instance.LocalInfo.Port, AppShareData.Instance.LocalInfo.LocalIp);
                        AppShareData.Instance.UdpServer = new IPEndPoint(IPAddress.Parse(AppShareData.Instance.ServerConfig.Ip), AppShareData.Instance.ServerConfig.Port);

                        //注册
                        RegisterEventHandles.Instance.SendRegisterMessage(new RegisterParams
                        {
                            ClientName = AppShareData.Instance.ClientConfig.Name,
                            GroupId = AppShareData.Instance.ClientConfig.GroupId,
                            LocalTcpPort = 0,
                            Mac = mac,
                            LocalIps = IPEndPoint.Parse(serverSocket.LocalEndPoint.ToString()).Address.ToString(),
                            Timeout = 5 * 1000,
                            Callback = (result) =>
                            {
                                AppShareData.Instance.LocalInfo.IsConnecting = false;
                                AppShareData.Instance.ClientConfig.GroupId = result.GroupId;
                                AppShareData.Instance.RemoteInfo.Ip = result.Ip;
                                AppShareData.Instance.RemoteInfo.ConnectId = result.Id;
                                AppShareData.Instance.LocalInfo.Connected = true;
                                AppShareData.Instance.LocalInfo.TcpConnected = true;
                                AppShareData.Instance.RemoteInfo.TcpPort = result.TcpPort;

                                OnRegisterChange?.Invoke(this, true);
                                callback?.Invoke(string.Empty);
                            },
                            FailCallback = (fail) =>
                            {
                                AppShareData.Instance.LocalInfo.IsConnecting = false;
                                OnRegisterChange?.Invoke(this, false);
                                callback?.Invoke(fail.Msg);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex.Message);
                        callback?.Invoke(ex.Message);
                        AppShareData.Instance.LocalInfo.IsConnecting = false;
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

        struct Tcpkeepalive
        {
            public uint onoff;
            public uint keepalivetime;
            public uint keepaliveinterval;
        }

        /// &lt;summary&gt;
        /// 结构体转byte数组
        /// &lt;/summary&gt;
        /// &lt;param name="structObj"&gt;要转换的结构体   &lt;/param&gt;
        /// &lt;returns&gt;转换后的byte数组&lt;/returns&gt;
        public static byte[] StructToBytes(object structObj)
        {
            //得到结构体的大小
            int size = Marshal.SizeOf(structObj);
            //创建byte数组
            byte[] bytes = new byte[size];
            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将结构体拷到分配好的内存空间
            Marshal.StructureToPtr(structObj, structPtr, false);
            //从内存空间拷到byte数组
            Marshal.Copy(structPtr, bytes, 0, size);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            //返回byte数组
            return bytes;
        }
    }
}
