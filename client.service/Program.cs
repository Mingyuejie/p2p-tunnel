using client.service.plugins.p2pPlugins.fileServer;
using client.service.plugins.p2pPlugins.forward.tcp;
using client.service.plugins.punchHolePlugins;
using client.service.plugins.serverPlugins;
using client.service.plugins.serverPlugins.register;
using client.service.servers.clientServer;
using client.service.servers.clientServer.plugins;
using client.service.servers.webServer;
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

            ServiceCollection serviceCollection = new ServiceCollection();
            ServiceProvider serviceProvider = null;

            //配置文件注入
            Config config = Config.ReadConfig();
            serviceCollection.AddSingleton((e) => config);
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);

            serviceCollection.AddServerPlugin()
                .AddPunchHolePlugin()//打洞
                .AddFileServerPlugin()//文件服务
                .AddTcpForwardPlugin()  //tcp转发

                .AddClientServer() //客户端管理
                .AddUpnpPlugin()//upnp映射

                .AddWebServer();//客户端页面

            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider.UseServerPlugin()
                .UsePunchHolePlugin()//打洞
                .UseFileServerPlugin() //文件服务
                .UseTcpForwardPlugin()//tcp转发

                .UseClientServer()//客户端管理
                .UseUpnpPlugin()//upnp映射

                .UseWebServer();//客户端页面

            //自动注册
            if (config.Client.AutoReg)
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
