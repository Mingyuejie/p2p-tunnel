using client.service.album;
using client.service.cmd;
using client.service.ftp;
using client.service.ftp.server;
using client.service.plugins.punchHolePlugins;
using client.service.plugins.serverPlugins;
using client.service.plugins.serverPlugins.register;
using client.service.servers.clientServer;
using client.service.servers.clientServer.plugins;
using client.service.servers.webServer;
using client.service.tcpforward;
using client.service.upnp;
using client.service.wakeup;
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

            //外部程序集的插件
            var externalAddembly = new[] {
                typeof(AlbumSettingPlugin).Assembly,
                typeof(TcpForwardPlugin).Assembly,
                typeof(UpnpPlugin).Assembly,
                typeof(FtpServerPlugin).Assembly,
                typeof(WakeUpPlugin).Assembly,
                typeof(CmdPlugin).Assembly,
            };

            serviceCollection
                //基础的功能
                .AddServerPlugin()
                .AddPunchHolePlugin()//打洞
                .AddClientServer() //客户端管理
                .AddWebServer()//客户端页面

                //外部插件
                .AddServerPlugin(externalAddembly).AddClientServer(externalAddembly)
                .AddTcpForwardPlugin()  //tcp转发
                .AddAlbumPlugin() //图片相册插件
                .AddUpnpPlugin()//upnp映射
                .AddFtpPlugin() //文件服务
                .AddCmdPlugin()
                ;


            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider
                //基础的功能
                .UseServerPlugin()
                .UsePunchHolePlugin()//打洞
                .UseClientServer()//客户端管理
                .UseWebServer()//客户端页面

                //外部插件
                .UseServerPlugin(externalAddembly).UseClientServer(externalAddembly)
                .UseTcpForwardPlugin()//tcp转发
                .UseAlbumPlugin() //图片相册插件
                .UseUpnpPlugin()//upnp映射
                .UseFtpPlugin() //文件服务
                .UseCmdPlugin()
               ;
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
