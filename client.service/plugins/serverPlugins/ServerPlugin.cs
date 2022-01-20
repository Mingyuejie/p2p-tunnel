using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.clients;
using client.service.plugins.serverPlugins.heart;
using client.service.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.reset;
using common;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.achieves.defaults;
using server.plugin;
using server.plugins.register.caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace client.service.plugins.serverPlugins
{
    public static class ServerPlugin
    {
        public static ServiceCollection AddServerPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            obj.AddSingleton<ITcpServer, TCPServer>();
            obj.AddSingleton<IUdpServer, UDPServer>();

            obj.AddSingleton<IClientInfoCaching, ClientInfoCache>();

            obj.AddSingleton<IServerRequest, ServerRequestHelper>();

            obj.AddSingleton<ResetMessageHelper>();
            obj.AddSingleton<HeartMessageHelper>();
            obj.AddSingleton<ClientsHelper>();
            obj.AddSingleton<ClientsMessageHelper>();
            obj.AddSingleton<RegisterMessageHelper>();
            obj.AddSingleton<RegisterHelper>();
            obj.AddSingleton<RegisterState>();
            obj.AddSingleton<ITunnelRegister, TunnelRegister>();
            

            obj.AddSingleton<ServerPluginHelper>();

            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray(), typeof(IPlugin)))
            {
                obj.AddSingleton(item);
            }
            return obj;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            obj.GetService<ClientsHelper>();

            ServerPluginHelper serverPluginHelper = obj.GetService<ServerPluginHelper>();

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray(), typeof(IPlugin)))
            {
                serverPluginHelper.LoadPlugin(item, obj.GetService(item));
            }
            return obj;
        }
    }
}
