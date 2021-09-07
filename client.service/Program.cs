using client.service.clientService;
using client.service.clientService.plugins;
using client.service.config;
using client.service.p2pPlugins.plugins.forward.tcp;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.register;
using client.service.webServer;
using common;
using common.extends;
using System;
using System.IO;

namespace client.service
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance.Info("正在启动...");

            Config config = File.ReadAllText("appsettings.json").DeJson<Config>();

            //客户端信息
            AppShareData.Instance.ClientConfig = config.Client;
            //服务器信息
            AppShareData.Instance.ServerConfig = config.Server;
            AppShareData.Instance.FileServerConfig = config.FileServer;

            //客户端websocket
            ClientServer.Instance.Start(config.Websocket.Ip, config.Websocket.Port);
            
            //端点
            ClientsHelper.Instance.Start();
            //TCP转发
            TcpForwardHelper.Instance.Start();
           
            //UPNP
            UpnpHelper.Instance.Start();
            
            //静态web服务
            WebServer.Instance.Start(config.Web.Ip, config.Web.Port, config.Web.Path);
           

            //退出事件
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                RegisterEventHandles.Instance.SendExitMessage();
            };
            Console.CancelKeyPress += (s, e) =>
            {
                RegisterEventHandles.Instance.SendExitMessage();
            };

            //自动注册
            if (AppShareData.Instance.ClientConfig.AutoReg)
            {
                RegisterHelper.Instance.AutoReg();
            }

            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning("没什么报红的报黄的，就说明运行成功了");
            Logger.Instance.Warning("=======================================");
            Logger.Instance.Warning($"前端管理地址:http://{config.Web.Ip}:{config.Web.Port}");
            Logger.Instance.Warning($"管理通信地址:ws://{config.Websocket.Ip}:{config.Websocket.Port}");

            Console.ReadLine();
        }
    }
}
