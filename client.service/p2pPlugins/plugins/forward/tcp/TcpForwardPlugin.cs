using common.extends;

namespace client.service.p2pPlugins.plugins.forward.tcp
{
    public class TcpForwardPlugin : IP2PPlugin
    {
        public P2PDataTypes Type => P2PDataTypes.TCP_FORWARD;
        public void Excute(OnP2PTcpArg arg)
        {
            TcpForwardModel data = arg.Data.Data.DeBytes<TcpForwardModel>();
            TcpForwardEventHandles.Instance.OnTcpForward(new OnTcpForwardEventArg
            {
                Packet = arg.Packet,
                Data = data,
            });
        }
    }
}
