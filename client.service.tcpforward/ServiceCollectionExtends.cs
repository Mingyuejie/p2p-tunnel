using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection services)
        {
            Config config = Config.ReadConfig().Result;
            services.AddSingleton((e) => config);

            services.AddSingleton<ITcpForwardServer, TcpForwardServer>();
            services.AddSingleton<TcpForwardHelper>();
            services.AddSingleton<TcpForwardMessengerSender>();
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
