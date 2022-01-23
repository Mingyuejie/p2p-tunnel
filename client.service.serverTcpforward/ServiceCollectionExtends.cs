using client.messengers.register;
using common;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.serverTcpforward
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddServerTcpForwardPlugin(this ServiceCollection services)
        {
            Config config = Config.ReadConfig().Result;
            services.AddSingleton((e) => config);
            services.AddSingleton<ServerTcpForwardHelper>();

            return services;
        }
        public static ServiceProvider UseServerTcpForwardPlugin(this ServiceProvider services)
        {

            var registerState = services.GetService<RegisterStateInfo>();
            var serverTcpForwardHelper = services.GetService<ServerTcpForwardHelper>();
            var config = services.GetService<Config>();
            registerState.LocalInfo.TcpConnectedSub.Sub((connected) =>
            {
                if (connected && config.AutoReg)
                {
                    TimerIntervalHelper.SetTimeout(() =>
                    {
                        serverTcpForwardHelper.UnRegister().Wait();
                        serverTcpForwardHelper.Register().Wait();
                    }, 1000);
                }

            });

            Logger.Instance.Info("服务器TCP转发插件已加载");
            return services;
        }
    }


}
