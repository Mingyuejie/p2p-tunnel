using client.service.plugins.serverPlugins.heart;
using client.service.plugins.serverPlugins.register;
using common;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using client.service.plugins.punchHolePlugins.plugins.udp;
using client.service.plugins.punchHolePlugins.plugins.tcp;
using client.service.plugins.punchHolePlugins;
using client.plugins.serverPlugins.register;
using client.plugins.serverPlugins.clients;
using server.plugins.register.caching;
using server;
using common.extends;

namespace client.service.plugins.serverPlugins.clients
{
    public class ClientsHelper
    {
        private long readClientsTimes = 0;

        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterState registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly HeartEventHandles heartEventHandles;
        private readonly ServerPluginHelper serverPluginHelper;

        public ClientsHelper(
            RegisterEventHandles registerEventHandles, ClientsEventHandles clientsEventHandles,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp,
            HeartEventHandles heartEventHandles, IClientInfoCaching clientInfoCaching,
            RegisterState registerState, PunchHoleEventHandles punchHoldEventHandles,
            ServerPluginHelper serverPluginHelper
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.heartEventHandles = heartEventHandles;
            this.serverPluginHelper = serverPluginHelper;

            UdpSubs();
            TcpSubs();
            Heart();

            //有人要求反向链接
            punchHoldEventHandles.OnReverse.Sub((arg) =>
           {
               if (clientInfoCaching.Get(arg.Data.Id, out ClientInfo client) && client != null)
               {
                   ConnectClient(client);
               }
           });
            //退出消息
            registerEventHandles.OnExitMessage.Sub((e) =>
            {
                readClientsTimes = 0;
                foreach (ClientInfo client in clientInfoCaching.All())
                {
                    clientInfoCaching.Remove(client.Id);
                    if (client.Connecting)
                    {
                        punchHoleTcp.SendStep2Stop(client.Id);
                    }
                }
            });
            //本客户端注册状态
            registerEventHandles.OnRegisterStateChange.Sub((e) =>
            {
                if (e.State)
                {
                    registerState.RemoteInfo.Ip = e.Ip;
                }
            });
            //收到来自服务器的 在线客户端 数据
            clientsEventHandles.OnData.Sub(OnServerSendClients);

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

        private void OnServerSendClients(OnServerSendClientsEventArg e)
        {
            try
            {
                if (!registerState.LocalInfo.TcpConnected) return;
                if (e.Data.Clients == null)
                {
                    return;
                }

                System.Threading.Interlocked.Increment(ref readClientsTimes);

                //Logger.Instance.Info(e.Data.Clients.ToJson());

                //下线了的
                IEnumerable<long> offlines = clientInfoCaching.AllIds().Except(e.Data.Clients.Select(c => c.Id));
                foreach (long offid in offlines)
                {
                    if (offid == registerState.RemoteInfo.ConnectId)
                    {
                        continue;
                    }
                    clientInfoCaching.OfflineBoth(offid);
                    clientInfoCaching.Remove(offid);
                }
                //新上线的
                IEnumerable<long> upLines = e.Data.Clients.Select(c => c.Id).Except(clientInfoCaching.AllIds());
                IEnumerable<ClientsClientModel> upLineClients = e.Data.Clients.Where(c => upLines.Contains(c.Id) && c.Id != registerState.RemoteInfo.ConnectId);

                foreach (ClientsClientModel item in upLineClients)
                {
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
                        TcpPort = item.TcpPort,
                        Mac = item.Mac,
                        SelfId = registerState.RemoteInfo.ConnectId
                    };
                    clientInfoCaching.Add(client);
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
        }


        public void ConnectClient(long id)
        {
            clientInfoCaching.Get(id, out ClientInfo client);
            if (client != null)
            {
                ConnectClient(client);
            }
        }
        private void ConnectClient(ClientInfo info)
        {
            Task.Run(() =>
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
                            clientInfoCaching.Online(e.Data.FromId, e.Packet.SourcePoint);
                        },
                        FailCallback = (e) =>
                        {
                            clientInfoCaching.Offline(info.Id);
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
                            clientInfoCaching.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket);
                        },
                        FailCallback = (e) =>
                        {
                            Logger.Instance.Error(e.Msg);
                            clientInfoCaching.OfflineTcp(info.Id);
                        },
                        Id = info.Id,
                        Name = info.Name,
                        Timeout = 300 * 1000
                    });
                }
            });
        }

        public void OfflineClient(long id)
        {
            clientInfoCaching.OfflineBoth(id);
        }
        public bool Get(long id, out ClientInfo client)
        {
            return clientInfoCaching.Get(id, out client);
        }

        private void UdpSubs()
        {
            punchHoleUdp.OnStep1Handler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
                {
                    cacheClient.Connecting = true;
                }
                _ = Helper.SetTimeout(() =>
                {
                    if (clientInfoCaching.Get(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null && !cacheClient.Connected)
                    {
                        cacheClient.Connecting = false;
                    }
                }, 3000);
            });
            punchHoleUdp.OnStep3Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Packet.SourcePoint); });
        }

        private void TcpSubs()
        {
            punchHoleTcp.OnStep1Handler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.Data.Id, out ClientInfo cacheClient) && cacheClient != null)
                {
                    cacheClient.TcpConnecting = true;
                }
            });
            punchHoleTcp.OnStep3Handler.Sub((e) => clientInfoCaching.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket));
            punchHoleTcp.OnStep4Handler.Sub((e) => clientInfoCaching.OnlineTcp(e.Data.FromId, e.Packet.TcpSocket));
            punchHoleTcp.OnStep2FailHandler.Sub((e) => clientInfoCaching.OfflineTcp(e.Data.FromId));
        }


        private void Heart()
        {
            //给各个客户端发送心跳包
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    foreach (ClientInfo client in clientInfoCaching.All())
                    {
                        if (client.IsTimeout())
                        {
                            if (client.Connected && !client.Connecting)
                            {
                                clientInfoCaching.Offline(client.Id);
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
                                clientInfoCaching.OfflineTcp(client.Id);
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
            heartEventHandles.OnHeart.Sub((e) =>
            {
                if (e.Data.SourceId > 0)
                {
                    if (e.Packet.ServerType == ServerType.UDP)
                    {
                        clientInfoCaching.UpdateLastTime(e.Data.SourceId);
                    }
                    else if (e.Packet.ServerType == ServerType.TCP)
                    {
                        clientInfoCaching.UpdateTcpLastTime(e.Data.SourceId);
                    }
                }
            });
        }
    }
}
