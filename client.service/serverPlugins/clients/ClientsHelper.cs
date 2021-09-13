﻿using client.service.config;
using client.service.punchHolePlugins;
using client.service.punchHolePlugins.plugins;
using client.service.punchHolePlugins.plugins.tcp;
using client.service.punchHolePlugins.plugins.tcp.nutssb;
using client.service.punchHolePlugins.plugins.udp;
using client.service.serverPlugins.heart;
using client.service.serverPlugins.register;
using common;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace client.service.serverPlugins.clients
{
    public class ClientsHelper
    {
        private short readClientsTimes = 0;

        private readonly RegisterEventHandles registerEventHandles;
        private readonly ClientsEventHandles clientsEventHandles;
        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly HeartEventHandles heartEventHandles;
        private readonly RegisterHelper registerHelper;
        private readonly RegisterState registerState;
        private readonly Config config;
        private readonly PunchHoleEventHandles punchHoldEventHandles;

        public IEnumerable<ClientInfo> Clients => ClientInfo.All();

        public ClientsHelper(RegisterEventHandles registerEventHandles,
            ClientsEventHandles clientsEventHandles,
           IPunchHoleUdp punchHoleUdp,
           IPunchHoleTcp punchHoleTcp,
            HeartEventHandles heartEventHandles, RegisterHelper registerHelper,
            RegisterState registerState, Config config, PunchHoleEventHandles punchHoldEventHandles)
        {
            this.registerEventHandles = registerEventHandles;
            this.clientsEventHandles = clientsEventHandles;
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.heartEventHandles = heartEventHandles;
            this.registerHelper = registerHelper;
            this.registerState = registerState;
            this.config = config;
            this.punchHoldEventHandles = punchHoldEventHandles;

            //本客户端注册状态
            registerEventHandles.OnSendRegisterTcpStateChangeHandler += OnRegisterTcpStateChangeHandler;
            //收到来自服务器的 在线客户端 数据
            clientsEventHandles.OnServerSendClientsHandler += OnServerSendClientsHandler;

            punchHoleUdp.OnStep1Handler += UdpOnStep1Handler;
            punchHoleUdp.OnStep3Handler += UdpOnStep3Handler;


            punchHoleTcp.OnStep3Handler += TcpOnStep3Handler;
            punchHoleTcp.OnStep4Handler += TcpOnStep4Handler;
            punchHoleTcp.OnSendTcpStep2FailHandler += TcpOnSendTcpStep2FailHandler;

            //有人要求反向链接
            punchHoldEventHandles.OnReverseHandler += (s, arg) =>
            {
                if (ClientInfo.Get(arg.Data.Id, out ClientInfo client) && client != null)
                {
                    ConnectClient(client);
                }
            };


            //退出消息
            registerEventHandles.OnSendExitMessageHandler += (sender, e) =>
            {
                foreach (ClientInfo client in ClientInfo.All())
                {
                    client.Remove();
                    if (client.Connecting)
                    {
                        punchHoleTcp.SendStep2Stop(client.Id);
                    }
                }
                readClientsTimes = 0;
            };

            //给各个客户端发送心跳包
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    foreach (ClientInfo client in ClientInfo.All())
                    {
                        if (client.IsTimeout())
                        {
                            if (client.Connected && !client.Connecting)
                            {
                                client.Offline();
                            }
                        }
                        else if (client.Connected)
                        {
                            heartEventHandles.SendHeartMessage(registerState.RemoteInfo.ConnectId, client.Address);
                        }

                        if (client.IsTcpTimeout())
                        {
                            if (client.TcpConnected && !client.TcpConnecting)
                            {
                                client.OfflineTcp();
                            }
                        }
                        else if (client.TcpConnected)
                        {
                            heartEventHandles.SendTcpHeartMessage(registerState.RemoteInfo.ConnectId, client.Socket);
                        }

                    }
                    System.Threading.Thread.Sleep(5000);
                }

            }, TaskCreationOptions.LongRunning);

            //收客户端的心跳包
            heartEventHandles.OnHeartEventHandler += (sender, e) =>
            {
                if (e.Data.SourceId > 0)
                {
                    if (e.Packet.ServerType == ServerType.UDP)
                    {
                        ClientInfo.UpdateLastTime(e.Data.SourceId);
                    }
                    else if (e.Packet.ServerType == ServerType.TCP)
                    {
                        ClientInfo.UpdateTcpLastTime(e.Data.SourceId);
                    }
                }
            };

            _ = Task.Run(() =>
            {
                try
                {
                    registerState.LocalInfo.RouteLevel = Helper.GetRouteLevel();
                }
                catch (Exception)
                {
                }
            });
        }

       

        private void TcpOnSendTcpStep2FailHandler(object sender, OnSendStep2FailEventArg e)
        {
            ClientInfo.OfflineTcp(e.ToId);
        }

        private void TcpOnStep3Handler(object sender, punchHolePlugins.plugins.tcp.OnStep3EventArg e)
        {
            ClientInfo.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket);
        }
        private void TcpOnStep4Handler(object sender, punchHolePlugins.plugins.tcp.OnStep4EventArg e)
        {
            ClientInfo.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket);
        }

        private void UdpOnStep3Handler(object sender, punchHolePlugins.plugins.udp.OnStep3EventArg e)
        {
            ClientInfo.Online(e.Data.FromId, e.Packet.SourcePoint);
        }

        private void UdpOnStep1Handler(object sender, punchHolePlugins.plugins.udp.OnStep1EventArg e)
        {
            if (ClientInfo.Get(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
            {
                cacheClient.Connecting = true;
            }
            _ = Helper.SetTimeout(() =>
            {
                if (ClientInfo.Get(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null && !cacheClient.Connected)
                {
                    cacheClient.Connecting = false;
                }
            }, 3000);
        }


        private void OnRegisterTcpStateChangeHandler(object sender, RegisterTcpEventArg e)
        {
            if (e.State)
            {
                registerState.RemoteInfo.Ip = e.Ip;
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
                    IEnumerable<long> offlines = ClientInfo.AllIds().Except(e.Data.Clients.Select(c => c.Id));
                    foreach (long offid in offlines)
                    {
                        if (offid == registerState.RemoteInfo.ConnectId)
                        {
                            continue;
                        }
                        ClientInfo.OfflineBoth(offid);
                        ClientInfo.Remove(offid);
                    }
                    //新上线的
                    IEnumerable<long> upLines = e.Data.Clients.Select(c => c.Id).Except(ClientInfo.AllIds());
                    IEnumerable<ClientsClientModel> upLineClients = e.Data.Clients.Where(c => upLines.Contains(c.Id));
                    foreach (ClientsClientModel item in upLineClients)
                    {
                        if (item.Id == registerState.RemoteInfo.ConnectId)
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
                        ClientInfo.Add(client);
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
            ClientInfo.Get(id, out ClientInfo client);
            if (client != null)
            {
                ConnectClient(client);
            }
        }

        private void ConnectClient(ClientInfo info)
        {
            if (info.Id == registerState.RemoteInfo.ConnectId)
            {
                return;
            }
            if (info.Connecting == false && info.Connected == false)
            {
                info.Connecting = true;
                punchHoleUdp.SendStep1(new ConnectParams
                {
                    Id = info.Id,
                    Name = info.Name,
                    Callback = (e) =>
                    {
                        ClientInfo.Online(e.Data.FromId, e.Packet.SourcePoint);
                    },
                    FailCallback = (e) =>
                    {
                        ClientInfo.Offline(info.Id);
                    },
                    Timeout = 2000,
                    TryTimes = 5
                });
            }

            if (info.TcpConnecting == false && info.TcpConnected == false)
            {
                info.TcpConnecting = true;
                punchHoleTcp.Send(new ConnectTcpParams
                {
                    Callback = (e) =>
                    {
                        ClientInfo.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket);
                    },
                    FailCallback = (e) =>
                    {
                        Logger.Instance.Error(e.Msg);
                        ClientInfo.OfflineTcp(info.Id);
                    },
                    Id = info.Id,
                    Name = info.Name,
                    Timeout = 300 * 1000
                });
            }
        }

        public void OfflineClient(long id)
        {
            ClientInfo.OfflineBoth(id);
        }
        public bool Get(long id, out ClientInfo client)
        {
            return ClientInfo.Get(id, out client);
        }
    }
}
