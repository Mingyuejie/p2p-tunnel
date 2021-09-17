using client.service.clientService;
using client.service.config;
using client.service.p2pPlugins.fileServer;
using client.service.p2pPlugins.forward.tcp;
using client.service.serverPlugins;
using client.service.serverPlugins.register;
using client.service.webServer;
using common;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace client.service
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance.Info("正在启动...");

            Config config = Config.ReadConfig();

            var serviceCollection = new ServiceCollection();
            ServiceProvider serviceProvider = null;

            serviceCollection.AddSingleton((e) => config);
            serviceCollection.AddSingleton((e) => serviceProvider);

            serviceCollection.AddServerPlugin()
                .AddFileServerPlugin()
                .AddTcpForwardPlugin()
                .AddClientServer()
                .AddWebServer();

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseServerPlugin()
                .UseFileServerPlugin()
                .UseTcpForwardPlugin()
                .UseClientServer()
                .UseWebServer();

            //自动注册
            if (serviceProvider.GetService<Config>().Client.AutoReg)
            {
                serviceProvider.GetService<RegisterHelper>().AutoReg();
            }

            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning($"前端管理地址:http://{config.Web.BindIp}:{config.Web.Port}");
            Logger.Instance.Warning($"管理通信地址:ws://{config.Websocket.BindIp}:{config.Websocket.Port}");

            Console.ReadLine();
        }
    }
}
