using client.service.tcpforward.client;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardPlugin : IPlugin
    {
        private readonly TcpForwardEventHandles tcpForwardEventHandles;
        public TcpForwardPlugin(TcpForwardEventHandles tcpForwardEventHandles)
        {
            this.tcpForwardEventHandles = tcpForwardEventHandles;
        }

        public async Task Execute(PluginParamWrap arg)
        {
            await tcpForwardEventHandles.OnTcpForward(new OnTcpForwardEventArg
            {
                Packet = arg,
                Data = arg.Wrap.Memory.DeBytes<TcpForwardModel>(),
            });
        }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection obj)
        {
            TcpForwardSettingModel config = TcpForwardSettingModel.ReadConfig().Result;
            obj.AddSingleton((e) => config);

            obj.AddSingleton<ITcpForwardServer, TcpForwardServer>();
            obj.AddSingleton<TcpForwardHelper>();
            obj.AddSingleton<TcpForwardEventHandles>();
            obj.AddSingleton<ConnectPool>();

            return obj;
        }
        public static ServiceProvider UseTcpForwardPlugin(this ServiceProvider obj)
        {
            obj.GetService<TcpForwardHelper>().Start();

            return obj;
        }
    }
}
