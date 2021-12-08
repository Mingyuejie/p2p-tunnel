using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using client.service.plugins.punchHolePlugins;
using client.service.plugins.serverPlugins.clients;
using client.service.plugins.serverPlugins.heart;
using client.service.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.reset;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.achieves.async;
using server.achieves.defaults;
using server.achieves.IOCP;
using server.plugin;
using server.plugins.register.caching;
using System;
using System.Linq;
using System.Reflection;

namespace client.service.plugins.serverPlugins
{
    public static class ServerPlugin
    {
        public static ServiceCollection AddServerPlugin(this ServiceCollection obj)
        {

            obj.AddServerPlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.AddSingleton<ITcpServer, TCPServer>();
            obj.AddSingleton<IUdpServer, UDPServer>();

            obj.AddSingleton<IClientInfoCaching, ClientInfoCache>();


            obj.AddSingleton<IServerRequest, ServerRequestHelper>();

            obj.AddSingleton<ResetEventHandles>();
            obj.AddSingleton<HeartEventHandles>();
            obj.AddSingleton<ClientsHelper>();
            obj.AddSingleton<ClientsEventHandles>();
            obj.AddSingleton<RegisterEventHandles>();
            obj.AddSingleton<RegisterHelper>();
            obj.AddSingleton<RegisterState>();

            return obj;
        }

        public static ServiceCollection AddServerPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            obj.AddSingleton<ServerPluginHelper>();
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }
            return obj;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider obj)
        {
            obj.UseServerPlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.GetService<ClientsHelper>();

            return obj;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            ServerPluginHelper serverPluginHelper = obj.GetService<ServerPluginHelper>();
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (var item in types)
            {
                serverPluginHelper.LoadPlugin(item, obj.GetService(item));
            }
            return obj;
        }
    }
}
