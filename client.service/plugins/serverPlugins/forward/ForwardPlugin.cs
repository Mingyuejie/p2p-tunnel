using common.extends;
using server;
using server.model;
using server.packet;
using server.plugin;

namespace client.service.plugins.serverPlugins.forward
{
    /// <summary>
    /// 来自服务器的消息转发  a->服务器->b
    /// </summary>
    public class ForwardPlugin : IPlugin
    {
        private readonly ServerPluginHelper serverPluginHelper;
        public ForwardPlugin(ServerPluginHelper serverPluginHelper)
        {
            this.serverPluginHelper = serverPluginHelper;
        }

        public void Execute(PluginParamWrap data)
        {
            ForwardModel model = data.Wrap.Memory.DeBytes<ForwardModel>();

            IPacket packet = data.Packet;
            packet.Chunk = model.Data;
            serverPluginHelper.InputData(packet, new ServerDataWrap<IPacket>
            {
                Connection = data.Connection,
                Data = packet
            });

        }
    }
}
