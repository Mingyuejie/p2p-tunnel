using client.service.clientService;
using client.service.clientService.plugins;
using client.service.config;
using client.service.p2pPlugins.plugins.forward.tcp;
using client.service.serverPlugins;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.register;
using client.service.webServer;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server;
using System;
using System.IO;

namespace client.service
{
    class Program
    {
        public static ServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            Logger.Instance.Info("正在启动...");

            Config config = Config.ReadConfig();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton((e) => config);
            serviceCollection.AddServerPlugin().AddClientServer().AddWebServer();

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseServerPlugin().UseClientServer().UseWebServer();

            //自动注册
            if (serviceProvider.GetService<Config>().Client.AutoReg)
            {
                serviceProvider.GetService<RegisterHelper>().AutoReg();
            }

            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning($"前端管理地址:http://{config.Web.Ip}:{config.Web.Port}");
            Logger.Instance.Warning($"管理通信地址:ws://{config.Websocket.Ip}:{config.Websocket.Port}");

            Console.ReadLine();
        }
    }
}
