using common.extends;

namespace client.service.p2pPlugins.plugins.forward.tcp
{
    public class TcpForwardPlugin : IP2PMessagePlugin
    {
        public P2PDataMessageTypes Type => P2PDataMessageTypes.TCP_FORWARD;
        public void Excute(OnP2PTcpMessageArg arg)
        {
            TcpForwardModel data = arg.Data.Data.ProtobufDeserialize<TcpForwardModel>();
            TcpForwardEventHandles.Instance.OnTcpForwardMessage(new OnTcpForwardMessageEventArg
            {
                Packet = arg.Packet,
                Data = data,
            });
        }
    }
}
