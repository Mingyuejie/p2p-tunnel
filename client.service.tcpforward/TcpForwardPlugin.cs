using client.service.tcpforward.client;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server;
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

        public async Task Execute(IConnection connection)
        {
            var data = connection.ReceiveRequestWrap.Memory.DeBytes<TcpForwardModel>();
            await tcpForwardEventHandles.OnTcpForward(new OnTcpForwardEventArg
            {
                Connection = connection,
                Data = data,
            });
        }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection services)
        {
            TcpForwardSettingModel config = TcpForwardSettingModel.ReadConfig().Result;
            services.AddSingleton((e) => config);

            services.AddSingleton<ITcpForwardServer, TcpForwardServer>();
            services.AddSingleton<TcpForwardHelper>();
            services.AddSingleton<TcpForwardEventHandles>();
            services.AddSingleton<ConnectPool>();

            return services;
        }
        public static ServiceProvider UseTcpForwardPlugin(this ServiceProvider services)
        {
            services.GetService<TcpForwardHelper>().Start();

            return services;
        }
    }
}
