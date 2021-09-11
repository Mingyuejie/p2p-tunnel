using common.extends;
using server.model;
using server.plugin;
using server.service.cache;

namespace server.service.plugins
{
    public class HeartPlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        public HeartPlugin(IClientRegisterCache clientRegisterCache, ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
        }

        public MessageTypes MsgType => MessageTypes.HEART;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            HeartModel model = data.Packet.Chunk.DeBytes<HeartModel>();

            if (!clientRegisterCache.Verify(model.SourceId, data)) return;

            if (serverType == ServerType.UDP)
            {
                udpServer.Send(new RecvQueueModel<IModelBase>
                {
                    Address = data.SourcePoint,
                    Data = new HeartModel { TargetId = model.SourceId, SourceId = -1 }
                });
            }
            else if (serverType == ServerType.TCP)
            {
                tcpServer.Send(new RecvQueueModel<IModelBase>
                {
                    Address = data.SourcePoint,
                    TcpCoket = data.TcpSocket,
                    Data = new HeartModel { TargetId = model.SourceId, SourceId = -1 }
                });
            }
            clientRegisterCache.UpdateTime(model.SourceId);
        }
    }
}
