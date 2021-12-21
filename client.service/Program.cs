//using client.service.album;
using client.service.cmd;
using client.service.ddns;
using client.service.ddns.client;
using client.service.ftp;
using client.service.ftp.server;
using client.service.logger;
using client.service.plugins.punchHolePlugins;
using client.service.plugins.serverPlugins;
using client.service.plugins.serverPlugins.register;
using client.service.servers.clientServer;
using client.service.servers.webServer;
using client.service.serverTcpforward;
using client.service.tcpforward;
using client.service.upnp;
using client.service.wakeup;
using common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace client.service
{
    class Program
    {
        static void Main(string[] args)
        {
            Startup.Start();
            Console.ReadLine();
        }
    }

    class Startup
    {
        public static void Start()
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
            System.Reflection.Assembly[] assemblys = new[] {
                typeof(TcpForwardPlugin).Assembly,
                typeof(UpnpPlugin).Assembly,
                typeof(FtpServerPlugin).Assembly,
                typeof(WakeUpPlugin).Assembly,
                typeof(CmdPlugin).Assembly,
                typeof(LoggerPlugin).Assembly,
                typeof(ServerTcpForwardPlugin).Assembly,
                typeof(DdnsPlugin).Assembly,
            };

            serviceCollection
                //基础的功能
                .AddServerPlugin(assemblys)
                //客户端管理
                .AddClientServer(assemblys)

                .AddWebServer()//客户端页面
                .AddPunchHolePlugin()//打洞
                .AddTcpForwardPlugin()  //客户端tcp转发
                .AddUpnpPlugin()//upnp映射
                .AddFtpPlugin() //文件服务
                .AddCmdPlugin() //远程命令
                .AddLoggerPlugin() //日志
                .AddServerTcpForwardPlugin()//服务器TCP转发
                .AddDdnsPlugin()
            ;


            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider
                //基础的功能
                .UseServerPlugin(assemblys)
                //客户端管理
                .UseClientServer(assemblys)

                .UseWebServer()//客户端页面
                .UsePunchHolePlugin()//打洞
                .UseTcpForwardPlugin()//客户端tcp转发
                .UseUpnpPlugin()//upnp映射
                .UseFtpPlugin() //文件服务
                .UseCmdPlugin() //远程命令
                .UseLoggerPlugin() //日志
                .UseServerTcpForwardPlugin()//服务器TCP转发
                .UseDdnsPlugin()
               ;
            //自动注册
            serviceProvider.GetService<RegisterHelper>().AutoReg();

            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning($"前端管理地址:http://{config.Web.BindIp}:{config.Web.Port}");
            Logger.Instance.Warning($"管理通信地址:ws://{config.Websocket.BindIp}:{config.Websocket.Port}");

            ThreadPool.SetMaxThreads(65535, 65535);
        }
    }
}
