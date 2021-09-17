using client.service.events;
using client.service.p2pPlugins;
using client.service.punchHolePlugins;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.heart;
using client.service.serverPlugins.register;
using client.service.serverPlugins.reset;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.plugin;
using System;
using System.Linq;
using System.Reflection;

namespace client.service.serverPlugins
{
    public static class ServerPlugin
    {
        public static ServiceCollection AddServerPlugin(this ServiceCollection obj)
        {

            obj.AddServerPlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.AddSingleton<ITcpServer, TCPServer>();
            obj.AddSingleton<IUdpServer, UDPServer>();
            obj.AddSingleton<EventHandlers>();

            obj.AddSingleton<ResetEventHandles>();
            obj.AddSingleton<HeartEventHandles>();
            obj.AddSingleton<ClientsHelper>();
            obj.AddSingleton<ClientsEventHandles>();
            obj.AddSingleton<RegisterEventHandles>();
            obj.AddSingleton<RegisterHelper>();
            obj.AddSingleton<RegisterState>();

            obj.AddPunchHolePlugin();

            return obj;
        }

        public static ServiceCollection AddServerPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
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

            obj.UsePunchHolePlugin();

            return obj;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (var item in types)
            {
                Plugin.LoadPlugin(item,obj.GetService(item));
            }
            return obj;
        }
    }
}
