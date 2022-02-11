using client.service.ftp;
using client.service.ftp.server;
using client.service.logger;
using client.service.messengers.register;
using client.service.plugins.serverPlugins;
using client.service.servers.clientServer;
using client.service.servers.webServer;
using client.service.tcpforward;
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
            Config config = Config.ReadConfig().Result;
            serviceCollection.AddSingleton((e) => config);
            //注入 依赖注入服务供应 使得可以在别的地方通过注入的方式获得 ServiceProvider 以用来获取其它服务
            serviceCollection.AddSingleton((e) => serviceProvider);

            //外部程序集的插件
            System.Reflection.Assembly[] assemblys = new[] {
                typeof(TcpForwardMessenger).Assembly,
                //typeof(UpnpClientService).Assembly,
                typeof(FtpServerMessenger).Assembly,
                //typeof(WakeUpClientService).Assembly,
                //typeof(CmdMessenger).Assembly,
                typeof(LoggerClientService).Assembly,
                //typeof(ServerTcpForwardMessenger).Assembly,
                //typeof(DdnsClientService).Assembly,
            };

            serviceCollection
                .AddServerPlugin(assemblys)//基础的功能
                .AddClientServer(assemblys)//客户端管理
                .AddWebServer()//客户端页面
                .AddTcpForwardPlugin()  //客户端tcp转发
                //.AddUpnpPlugin()//upnp映射
                .AddFtpPlugin() //文件服务
                //.AddCmdPlugin() //远程命令
                .AddLoggerPlugin() //日志
                //.AddServerTcpForwardPlugin()//服务器TCP转发
                //.AddDdnsPlugin()
            ;


            serviceProvider = serviceCollection.BuildServiceProvider();
            serviceProvider
                .UseServerPlugin(assemblys)//基础的功能
                .UseClientServer(assemblys)//客户端管理
                .UseWebServer()//客户端页面
                .UseTcpForwardPlugin()//客户端tcp转发
                //.UseUpnpPlugin()//upnp映射
                .UseFtpPlugin() //文件服务
                //.UseCmdPlugin() //远程命令
                .UseLoggerPlugin() //日志
                //.UseServerTcpForwardPlugin()//服务器TCP转发
                //.UseDdnsPlugin()
               ;
            //自动注册
            _ = serviceProvider.GetService<RegisterHelper>().AutoReg();

            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning("没什么报红的，就说明运行成功了");
            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning($"前端管理地址:http://{config.Web.BindIp}:{config.Web.Port}");
            Logger.Instance.Warning($"管理通信地址:ws://{config.Websocket.BindIp}:{config.Websocket.Port}");

            ThreadPool.SetMaxThreads(65535, 65535);
        }
    }
}
