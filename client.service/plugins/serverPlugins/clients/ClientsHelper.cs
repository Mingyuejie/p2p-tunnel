using client.service.plugins.serverPlugins.heart;
using common;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using client.service.plugins.punchHolePlugins.plugins.udp;
using client.service.plugins.punchHolePlugins.plugins.tcp;
using client.service.plugins.punchHolePlugins;
using client.plugins.serverPlugins.register;
using client.plugins.serverPlugins.clients;
using server.plugins.register.caching;
using Newtonsoft.Json;
using common.extends;

namespace client.service.plugins.serverPlugins.clients
{
    public class ClientsHelper
    {
        private BoolSpace firstClients = new BoolSpace(true);

        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterState registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly HeartMessageHelper heartMessageHelper;

        public ClientsHelper(ClientsMessageHelper clientsMessageHelper,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp,
            HeartMessageHelper heartMessageHelper, IClientInfoCaching clientInfoCaching,
            RegisterState registerState, PunchHoleEventHandles punchHoldEventHandles
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.heartMessageHelper = heartMessageHelper;

            punchHoleUdp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, e.Packet.Connection));
            punchHoleUdp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Offline(e.RawData.FromId, e.Packet.Connection));
            punchHoleUdp.OnStep3Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Packet.Connection); });
            punchHoleUdp.OnStep4Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Packet.Connection); });

            punchHoleTcp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, e.Packet.Connection));
            punchHoleTcp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Offline(e.RawData.FromId, e.Packet.Connection));
            punchHoleTcp.OnStep3Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Packet.Connection));
            punchHoleTcp.OnStep4Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Packet.Connection));

            //有人要求反向链接
            punchHoldEventHandles.OnReverse.Sub(OnReverse);
            //本客户端注册状态
            registerState.OnRegisterStateChange.Sub(OnRegisterStateChange);
            //收到来自服务器的 在线客户端 数据
            clientsMessageHelper.OnServerClientsData.Sub(OnServerSendClients);

            Heart();
            Task.Run(() =>
            {
                registerState.LocalInfo.RouteLevel = NetworkHelper.GetRouteLevel();
            });
        }

        public void ConnectClient(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }
        private void ConnectClient(ClientInfo info)
        {
            if (info.Id == registerState.ConnectId)
            {
                return;
            }

            Task.Run(async () =>
            {
                await ConnectUdp(info);
                await ConnectTcp(info);
            });
        }
        private async Task ConnectUdp(ClientInfo info)
        {
            if (info.Connecting == false && info.Connected == false)
            {
                info.Connecting = true;
                var result = await punchHoleUdp.Send(new ConnectParams
                {
                    Id = info.Id,
                    Name = info.Name,
                    TryTimes = 5
                });
                if (!result.State)
                {
                    Logger.Instance.Error((result.Result as punchHolePlugins.plugins.udp.ConnectFailModel).Msg);
                    clientInfoCaching.Offline(info.Id);
                }
            }
        }
        private async Task ConnectTcp(ClientInfo info)
        {
            if (info.TcpConnecting == false && info.TcpConnected == false)
            {
                info.TcpConnecting = true;
                var result = await punchHoleTcp.Send(new ConnectTcpParams
                {
                    Id = info.Id,
                    Name = info.Name,
                    TryTimes = 5
                });
                if (!result.State)
                {
                    Logger.Instance.Error((result.Result as punchHolePlugins.plugins.tcp.ConnectFailModel).Msg);
                    clientInfoCaching.Offline(info.Id);
                }
            }
        }

        private void OnReverse(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }

        private void OnRegisterStateChange(bool state)
        {
            if (!state)
            {
                firstClients.Reset();
                foreach (ClientInfo client in clientInfoCaching.All())
                {
                    clientInfoCaching.Remove(client.Id);
                    if (client.Connecting)
                    {
                        punchHoleTcp.SendStep2Stop(client.Id);
                    }
                }
            }
        }
        private void OnServerSendClients(OnServerSendClientsEventArg e)
        {
            try
            {
                if (!registerState.LocalInfo.TcpConnected || e.Data.Clients == null)
                {
                    return;
                }

                //Logger.Instance.Info(JsonConvert.SerializeObject(e.Data.Clients));

                IEnumerable<ulong> remoteIds = e.Data.Clients.Select(c => c.Id);
                //下线了的
                IEnumerable<ulong> offlines = clientInfoCaching.AllIds().Except(remoteIds).Where(c => c != registerState.ConnectId);
                foreach (ulong offid in offlines)
                {
                    clientInfoCaching.Offline(offid);
                    clientInfoCaching.Remove(offid);
                }
                //新上线的
                IEnumerable<ulong> upLines = remoteIds.Except(clientInfoCaching.AllIds());
                IEnumerable<ClientsClientModel> upLineClients = e.Data.Clients.Where(c => upLines.Contains(c.Id) && c.Id != registerState.ConnectId);

                foreach (ClientsClientModel item in upLineClients)
                {
                    ClientInfo client = new ClientInfo
                    {
                        Connected = false,
                        TcpConnected = false,
                        Connecting = false,
                        UdpConnection = null,
                        TcpConnection = null,
                        Id = item.Id,
                        Name = item.Name,
                        Mac = item.Mac
                    };
                    clientInfoCaching.Add(client);
                    if (firstClients.Get())
                    {
                        ConnectClient(client);
                    }
                }

                firstClients.Reverse();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private void Heart()
        {
            //给各个客户端发送心跳包
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    long time = DateTimeHelper.GetTimeStamp();

                    foreach (ClientInfo client in clientInfoCaching.All())
                    {
                        if (client.UdpConnection != null)
                        {
                            if (client.UdpConnection.IsTimeout(time))
                            {
                                clientInfoCaching.Offline(client.Id);
                            }
                            else if (client.Connected && client.UdpConnection.IsNeedHeart(time))
                            {
                                await heartMessageHelper.SendHeartMessage(client.UdpConnection);
                            }
                        }
                    }
                    await Task.Delay(5000);
                }

            }, TaskCreationOptions.LongRunning);
        }
    }
}
