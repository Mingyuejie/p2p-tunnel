using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.cache;
using server.service.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.service.plugins
{
    public class PunchHolePlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        public PunchHolePlugin(IClientRegisterCache clientRegisterCache, ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_PUNCH_HOLE;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            PunchHoleModel model = data.Packet.Chunk.DeBytes<PunchHoleModel>();

            //A已注册
            RegisterCacheModel source = clientRegisterCache.Get(model.Id);
            if (source == null) return;
            //B已注册
            RegisterCacheModel target = clientRegisterCache.Get(model.ToId);
            if (target == null) return;

            //是否在同一个组
            if (source.GroupId != target.GroupId) return;

            if (model.PunchForwardType == PunchForwardTypes.NOTIFY)
            {
                model.Data = new PunchHoleNotifyModel
                {
                    Ip = source.Address.Address.ToString(),
                    Id = source.Id,
                    Name = source.Name,
                    Port = source.Address.Port,
                    TcpPort = source.TcpPort,
                    LocalIps = source.LocalIps,
                    LocalTcpPort = source.LocalTcpPort,
                    LocalUdpPort = source.LocalUdpPort,
                }.ToBytes();
            }
            switch (serverType)
            {
                case ServerType.TCP:
                    {
                        tcpServer.Send(new RecvQueueModel<IModelBase>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model
                        });
                    }
                    break;
                case ServerType.UDP:
                    {
                        udpServer.Send(new RecvQueueModel<IModelBase>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model
                        });
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
