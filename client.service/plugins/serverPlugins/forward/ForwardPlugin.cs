using common.extends;
using server;
using server.model;
using server.packet;
using server.plugin;

namespace client.service.plugins.serverPlugins.forward
{
    public class ForwardPlugin : IPlugin
    {
        private readonly ServerPluginHelper serverPluginHelper;
        public ForwardPlugin(ServerPluginHelper serverPluginHelper)
        {
            this.serverPluginHelper = serverPluginHelper;
        }

        public void Excute(PluginParamWrap data)
        {
            ForwardModel model = data.Wrap.Content.DeBytes<ForwardModel>();

            IPacket packet = data.Packet;
            packet.Chunk = model.Data;
            serverPluginHelper.InputData(packet, new ServerDataWrap<IPacket>
            {
                Address = data.SourcePoint,
                ServerType = data.ServerType,
                Socket = data.TcpSocket,
                Data = packet
            });

        }
    }
}
