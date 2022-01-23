using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.service.messengers.heart;
using client.service.messengers.punchHole;
using common;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.messengers.clients
{
    public class ClientsHelper
    {
        private BoolSpace firstClients = new BoolSpace(true);

        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly HeartMessengerSender  heartMessengerClient;

        public ClientsHelper(ClientsMessengerSender clientsMessageHelper,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp,
            HeartMessengerSender heartMessengerClient, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoldEventHandles
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.heartMessengerClient = heartMessengerClient;

            punchHoleUdp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.UDP));
            punchHoleUdp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Offline(e.RawData.FromId, ServerType.UDP));
            punchHoleUdp.OnStep3Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection); });
            punchHoleUdp.OnStep4Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection); });

            punchHoleTcp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.TCP));
            punchHoleTcp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Offline(e.RawData.FromId, ServerType.TCP));
            punchHoleTcp.OnStep3Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection));
            punchHoleTcp.OnStep4Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection));

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

        internal void ConnectClient(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }
        internal void ConnectClient(ClientInfo info)
        {
            if (info.Id == registerState.ConnectId)
            {
                return;
            }

            Task.Run(async () =>
            {
                if (info.UdpConnecting == false && info.UdpConnected == false)
                {
                    await ConnectUdp(info);
                    await ConnectTcp(info);
                }
            });
        }
        private async Task ConnectUdp(ClientInfo info)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.UDP);
            var result = await punchHoleUdp.Send(new ConnectUdpParams
            {
                Id = info.Id,
                TunnelName = "udp",
                TryTimes = 5
            });
            if (!result.State)
            {
                Logger.Instance.Error((result.Result as ConnectUdpFailModel).Msg);
                clientInfoCaching.Offline(info.Id);
            }
        }
        private async Task ConnectTcp(ClientInfo info)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.UDP);
            var result = await punchHoleTcp.Send(new ConnectTcpParams
            {
                Id = info.Id,
                TunnelName = "tcp",
                TryTimes = 5
            });
            if (!result.State)
            {
                Logger.Instance.Error((result.Result as ConnectTcpFailModel).Msg);
                clientInfoCaching.Offline(info.Id);
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
                    if (client.UdpConnecting)
                    {
                        punchHoleTcp.SendStep2Stop(client.Id);
                    }
                }
            }
        }
        private void OnServerSendClients(ClientsInfo clients)
        {
            try
            {
                if (!registerState.LocalInfo.TcpConnected || clients.Clients == null)
                {
                    return;
                }

                IEnumerable<ulong> remoteIds = clients.Clients.Select(c => c.Id);
                //下线了的
                IEnumerable<ulong> offlines = clientInfoCaching.AllIds().Except(remoteIds).Where(c => c != registerState.ConnectId);
                foreach (ulong offid in offlines)
                {
                    clientInfoCaching.Offline(offid);
                    clientInfoCaching.Remove(offid);
                }
                //新上线的
                IEnumerable<ulong> upLines = remoteIds.Except(clientInfoCaching.AllIds());
                IEnumerable<ClientsClientInfo> upLineClients = clients.Clients.Where(c => upLines.Contains(c.Id) && c.Id != registerState.ConnectId);

                foreach (ClientsClientInfo item in upLineClients)
                {
                    ClientInfo client = new ClientInfo
                    {
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
                            else if (client.UdpConnected && client.UdpConnection.IsNeedHeart(time))
                            {
                                await heartMessengerClient.Heart(client.UdpConnection);
                            }
                        }
                    }
                    await Task.Delay(5000);
                }

            }, TaskCreationOptions.LongRunning);
        }
    }
}
