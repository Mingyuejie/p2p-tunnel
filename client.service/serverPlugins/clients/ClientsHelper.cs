using client.service.serverPlugins.connectClient;
using client.service.serverPlugins.heart;
using client.service.serverPlugins.register;
using common;
using common.extends;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace client.service.serverPlugins.clients
{
    public class ClientsHelper
    {
        private static readonly Lazy<ClientsHelper> lazy = new(() => new ClientsHelper());
        public static ClientsHelper Instance => lazy.Value;
        private short readClientsTimes = 0;
        private readonly ConcurrentDictionary<long, ClientInfo> clients = AppShareData.Instance.Clients;
        private ClientsHelper()
        {
            //本客户端注册状态
            RegisterEventHandles.Instance.OnSendRegisterTcpStateChangeHandler += OnRegisterTcpStateChangeHandler;
            //收到来自服务器的 在线客户端 数据
            ClientsEventHandles.Instance.OnServerSendClientsHandler += OnServerSendClientsHandler;

            //UDP
            //有人想连接我
            ConnectClientEventHandles.Instance.OnConnectClientStep1Handler += OnConnectClientStep1Handler;
            //有人连接我
            ConnectClientEventHandles.Instance.OnConnectClientStep3Handler += OnConnectClientStep3Handler;

            //TCP
            //有人回应
            ConnectClientEventHandles.Instance.OnTcpConnectClientStep4Handler += OnTcpConnectClientStep4Handler;
            //连接别人失败
            ConnectClientEventHandles.Instance.OnSendTcpConnectClientStep2FailHandler += OnSendTcpConnectClientStep2FailHandler;

            //有人要求反向链接
            ConnectClientEventHandles.Instance.OnConnectClientReverseHandler += (s, arg) =>
            {
                if (clients.TryGetValue(arg.Data.Id, out ClientInfo client) && client != null)
                {
                    ConnectClient(client);
                }
            };


            //退出消息
            RegisterEventHandles.Instance.OnSendExitMessageHandler += (sender, e) =>
            {
                foreach (ClientInfo client in clients.Values)
                {
                    _ = RemoveClient(client.Id);
                    if (client.Connecting)
                    {
                        ConnectClientEventHandles.Instance.SendTcpConnectClientStep2StopMessage(client.Id);
                    }
                }
                clients.Clear();
                readClientsTimes = 0;
            };

            //给各个客户端发送心跳包
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    long time = Helper.GetTimeStamp();

                    foreach (ClientInfo client in clients.Values)
                    {
                        if (client.IsTimeout())
                        {
                            if (client.Connected && !client.Connecting)
                            {
                                SetClientOffline(client.Id);
                            }
                        }
                        else if (client.Connected)
                        {
                            HeartEventHandles.Instance.SendHeartMessage(client.Address);
                        }

                        if (client.IsTcpTimeout())
                        {
                            if (client.TcpConnected && !client.TcpConnecting)
                            {
                                SetClientTcpOffline(client.Id);
                            }
                        }
                        else if (client.TcpConnected)
                        {
                            Logger.Instance.Info($"给{client.Id}发送心跳包");
                            HeartEventHandles.Instance.SendTcpHeartMessage(client.Socket);
                        }

                    }
                    System.Threading.Thread.Sleep(5000);
                }

            }, TaskCreationOptions.LongRunning);

            //收客户端的心跳包
            HeartEventHandles.Instance.OnHeartEventHandler += (sender, e) =>
            {
                if (e.Data.SourceId > 0)
                {
                    _ = clients.TryGetValue(e.Data.SourceId, out ClientInfo cacheClient);
                    if (cacheClient != null)
                    {
                        if (e.Packet.ServerType == ServerType.UDP)
                        {
                            cacheClient.LastTime = Helper.GetTimeStamp();
                        }
                        else if (e.Packet.ServerType == ServerType.TCP)
                        {
                            Logger.Instance.Info($"收到{e.Data.SourceId}的心跳包");
                            cacheClient.TcpLastTime = Helper.GetTimeStamp();
                        }
                    }
                }
            };

            _ = Task.Run(() =>
            {
                try
                {
                    AppShareData.Instance.RouteLevel = Helper.GetRouteLevel();
                }
                catch (Exception)
                {
                }
            });
        }
        public void Start()
        {

        }

        private void OnConnectClientStep1Handler(object sender, OnConnectClientStep1EventArg e)
        {
            if (clients.TryGetValue(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
            {
                cacheClient.Connecting = true;
            }
            _ = Helper.SetTimeout(() =>
            {
                if (clients.TryGetValue(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null && !cacheClient.Connected)
                {
                    cacheClient.Connecting = false;
                }
            }, 3000);
        }
        private void OnConnectClientStep3Handler(object sender, OnConnectClientStep3EventArg e)
        {
            if (clients.TryGetValue(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
            {
                cacheClient.Connected = true;
                cacheClient.Connecting = false;
                cacheClient.LastTime = Helper.GetTimeStamp();
                cacheClient.Address = e.Packet.SourcePoint;
            }
        }


        private void OnSendTcpConnectClientStep2FailHandler(object sender, OnSendTcpConnectClientStep2FailEventArg e)
        {
            SetClientTcpOffline(e.ToId);
        }

        private void OnTcpConnectClientStep4Handler(object sender, OnConnectClientStep4EventArg e)
        {
            if (clients.TryGetValue(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
            {
                cacheClient.TcpConnected = true;
                cacheClient.TcpConnecting = false;
                cacheClient.TcpLastTime = Helper.GetTimeStamp();
                cacheClient.Socket = e.Packet.TcpSocket;
            }
        }

        private void OnRegisterTcpStateChangeHandler(object sender, RegisterTcpEventArg e)
        {
            if (e.State)
            {
                AppShareData.Instance.Ip = e.Ip;
            }
        }

        private void OnServerSendClientsHandler(object sender, OnServerSendClientsEventArg e)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    ++readClientsTimes;
                    if (e.Data.Clients == null)
                    {
                        return;
                    }

                    //下线了的
                    IEnumerable<long> offlines = clients.Values.Select(c => c.Id).Except(e.Data.Clients.Select(c => c.Id));
                    foreach (long offid in offlines)
                    {
                        if (offid == AppShareData.Instance.ConnectId)
                        {
                            continue;
                        }
                        SetClientOffline(offid);
                        SetClientTcpOffline(offid);
                        _ = RemoveClient(offid);
                    }
                    //新上线的
                    IEnumerable<long> upLines = e.Data.Clients.Select(c => c.Id).Except(clients.Values.Select(c => c.Id));
                    IEnumerable<MessageClientsClientModel> upLineClients = e.Data.Clients.Where(c => upLines.Contains(c.Id));
                    foreach (MessageClientsClientModel item in upLineClients)
                    {
                        if (item.Id == AppShareData.Instance.ConnectId)
                        {
                            continue;
                        }
                        ClientInfo client = new ClientInfo
                        {
                            LastTime = 0,
                            TcpLastTime = 0,
                            Connected = false,
                            TcpConnected = false,
                            Connecting = false,
                            Socket = null,
                            Address = IPEndPoint.Parse(item.Address),
                            Id = item.Id,
                            Name = item.Name,
                            Port = item.Port,
                            TcpPort = item.TcpPort
                        };
                        _ = clients.TryAdd(item.Id, client);
                        if (readClientsTimes == 1)
                        {
                            ConnectClient(client);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error("" + ex);
                }
            });
        }


        public void ConnectClient(long id)
        {
            clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                ConnectClient(client);
            }
        }

        private void ConnectClient(ClientInfo info)
        {
            if (info.Id == AppShareData.Instance.ConnectId)
            {
                return;
            }
            if (info.Connecting == false && info.Connected == false)
            {
                info.Connecting = true;
                ConnectClientEventHandles.Instance.SendConnectClientMessage(new ConnectParams
                {
                    Id = info.Id,
                    Name = info.Name,
                    Callback = (e) =>
                    {
                        if (clients.TryGetValue(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
                        {
                            cacheClient.Connected = true;
                            cacheClient.LastTime = Helper.GetTimeStamp();
                            cacheClient.Address = e.Packet.SourcePoint;
                            cacheClient.Connecting = false;
                        }
                    },
                    FailCallback = (e) =>
                    {
                        SetClientOffline(info.Id);
                    },
                    Timeout = 2000,
                    TryTimes = 5
                });
            }

            if (info.TcpConnecting == false && info.TcpConnected == false)
            {
                info.TcpConnecting = true;
                ConnectClientEventHandles.Instance.SendTcpConnectClientMessage(new ConnectTcpParams
                {
                    Callback = (e) =>
                    {
                        if (clients.TryGetValue(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
                        {
                            cacheClient.TcpConnected = true;
                            cacheClient.TcpConnecting = false;
                            cacheClient.TcpLastTime = Helper.GetTimeStamp();
                            cacheClient.Socket = e.Packet.TcpSocket;
                        }
                    },
                    FailCallback = (e) =>
                    {
                        Logger.Instance.Error(e.Msg);
                        SetClientTcpOffline(info.Id);
                        info.TcpConnecting = false;
                    },
                    Id = info.Id,
                    Name = info.Name,
                    Timeout = 300 * 1000
                });
            }
        }

        private void SetClientOffline(long id)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.Connecting = false;
                client.Connected = false;
                client.LastTime = 0;
            }

        }
        private void SetClientTcpOffline(long id)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.TcpConnecting = false;
                client.TcpConnected = false;
                client.TcpLastTime = 0;
                if (client.Socket != null)
                {
                    client.Socket.SafeClose();
                }
            }
        }
        private ClientInfo RemoveClient(long id)
        {
            _ = clients.TryRemove(id, out ClientInfo client);
            return client;
        }


        public void OfflineClient(long id)
        {
            SetClientOffline(id);
            SetClientTcpOffline(id);
        }
    }
}
