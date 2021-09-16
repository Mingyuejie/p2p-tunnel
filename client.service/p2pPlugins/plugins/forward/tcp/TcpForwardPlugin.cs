using common.extends;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.p2pPlugins.plugins.forward.tcp
{
    public class TcpForwardPlugin : IP2PPlugin
    {
        private readonly TcpForwardEventHandles tcpForwardEventHandles;
        public TcpForwardPlugin(TcpForwardEventHandles tcpForwardEventHandles)
        {
            this.tcpForwardEventHandles = tcpForwardEventHandles;
        }

        public P2PDataTypes Type => P2PDataTypes.TCP_FORWARD;
        public void Excute(OnP2PTcpArg arg)
        {
            TcpForwardModel data = arg.Data.Data.DeBytes<TcpForwardModel>();
            tcpForwardEventHandles.OnTcpForward(new OnTcpForwardEventArg
            {
                Packet = arg.Packet,
                Data = data,
            });
        }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection obj)
        {
            obj.AddSingleton<TcpForwardServer>();
            obj.AddSingleton<TcpForwardHelper>();
            obj.AddSingleton<TcpForwardEventHandles>();
            return obj;
        }
        public static ServiceProvider UseTcpForwardPlugin(this ServiceProvider obj)
        {
            obj.GetService<TcpForwardHelper>().Start();

            return obj;
        }
    }
}
