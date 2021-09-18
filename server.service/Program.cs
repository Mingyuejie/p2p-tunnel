using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace server.service
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = File.ReadAllText("appsettings.json").DeJson<Config>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton((e) => config);

            serviceCollection.AddPlugin().AddTcpServer().AddUdpServer();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UsePlugin().UseTcpServer().UseUdpServer();


            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning($"UDP端口:{config.Udp}");
            Logger.Instance.Warning($"TCP端口:{config.Tcp}");

            _ = Console.ReadLine();
        }
    }
}
