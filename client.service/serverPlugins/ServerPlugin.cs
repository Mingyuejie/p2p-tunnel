
using client.service.config;
using client.service.events;
using client.service.p2pPlugins;
using client.service.p2pPlugins.plugins.request;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.connectClient;
using client.service.serverPlugins.heart;
using client.service.serverPlugins.register;
using client.service.serverPlugins.reset;
using common;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverPlugins
{
    public static class ServerPlugin
    {
        public static ServiceCollection AddServerPlugin(this ServiceCollection obj)
        {
            obj.AddSingleton<ITcpServer, TCPServer>();
            obj.AddSingleton<IUdpServer, UDPServer>();
            obj.AddSingleton<EventHandlers>();
            obj.AddSingleton<ResetEventHandles>();
            obj.AddSingleton<RegisterEventHandles>();
            obj.AddSingleton<RegisterHelper>();
            obj.AddSingleton<RegisterState>();
            obj.AddSingleton<HeartEventHandles>();
            obj.AddSingleton<ClientsHelper>();
            obj.AddSingleton<ClientsEventHandles>();


            obj.AddSingleton<ResetPlugin>();
            obj.AddSingleton<RegisterResultPlugin>();
            obj.AddSingleton<HeartPlugin>();
            obj.AddSingleton<ServerSendClientsPlugin>();


            obj.AddSingleton<ConnectClientEventHandles>();
            obj.AddSingleton<ConnectClientReversePlugin>();
            obj.AddSingleton<ConnectClientStep1Plugin>();
            obj.AddSingleton<ConnectClientStep2Plugin>();
            obj.AddSingleton<ConnectClientStep2RetryPlugin>();
            obj.AddSingleton<ConnectClientStep2FailPlugin>();
            obj.AddSingleton<ConnectClientStep3Plugin>();
            obj.AddSingleton<ConnectClientStep4Plugin>();
            obj.AddSingleton<ConnectClientStep2StopPlugin>();

            obj.AddSingleton<P2PPlugin>();
            obj.AddP2PPlugin();

            return obj;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider obj)
        {
            Plugin.LoadPlugin(obj.GetService<ResetPlugin>());
            Plugin.LoadPlugin(obj.GetService<RegisterResultPlugin>());
            Plugin.LoadPlugin(obj.GetService<HeartPlugin>());
            Plugin.LoadPlugin(obj.GetService<ServerSendClientsPlugin>());

            Plugin.LoadPlugin(obj.GetService<ConnectClientReversePlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep1Plugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep2Plugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep2RetryPlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep2FailPlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep3Plugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep4Plugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep2StopPlugin>());

            Plugin.LoadPlugin(obj.GetService<P2PPlugin>());

            obj.GetService<ClientsHelper>();
            var registerEventHandles = obj.GetService<RegisterEventHandles>();
            //退出事件
            AppDomain.CurrentDomain.ProcessExit += (s, e) => registerEventHandles.SendExitMessage();
            Console.CancelKeyPress += (s, e) => registerEventHandles.SendExitMessage();
            if (obj.GetService<Config>().Client.AutoReg)
            {
                obj.GetService<RegisterHelper>().AutoReg();
            }
            obj.UseP2PPlugin();
            return obj;
        }
    }
}
