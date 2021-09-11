using common.extends;
using server.model;
using server.plugin;
using server.service.cache;
using server.service.model;

namespace server.service.plugins
{
    public class ResetPlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        public ResetPlugin(IClientRegisterCache clientRegisterCache, ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_RESET;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            ResetModel model = data.Packet.Chunk.DeBytes<ResetModel>();

            if (!clientRegisterCache.Verify(model.Id, data)) return;

            //A已注册
            RegisterCacheModel source = clientRegisterCache.Get(model.Id);
            if (source != null)
            {
                //B已注册
                RegisterCacheModel target = clientRegisterCache.Get(model.ToId);
                if (target != null)
                {
                    //是否在同一个组
                    if (source.GroupId != target.GroupId)
                    {
                        return;
                    }

                    if (serverType == ServerType.UDP)
                    {
                        udpServer.Send(new RecvQueueModel<IModelBase>
                        {
                            Address = target.Address,
                            TcpCoket = null,
                            Data = model
                        });
                    }
                    else if (serverType == ServerType.TCP)
                    {
                        tcpServer.Send(new RecvQueueModel<IModelBase>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model
                        });
                    }
                }
            }
        }
    }
}
