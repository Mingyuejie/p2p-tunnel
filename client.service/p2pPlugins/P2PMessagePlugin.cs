using common.extends;
using server.model;
using server.models;
using server.plugin;

namespace client.service.p2pPlugins
{
    /// <summary>
    /// p2p消息总线
    /// </summary>
    public class P2PMessagePlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.P2P;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            MessageP2PModel data = model.Packet.Chunk.ProtobufDeserialize<MessageP2PModel>();

            if (serverType == ServerType.TCP)
            {
                P2PMessageEventHandles.Instance.OnTcpMessage(new OnP2PTcpMessageArg
                {
                    Data = data,
                    Packet = model
                });
            }
            else
            {
                P2PMessageEventHandles.Instance.OnMessage(new OnP2PTcpMessageArg
                {
                    Data = data,
                    Packet = model
                });
            }
        }
    }
}
