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
        public static ServiceCollection AddServerPlugin(this ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<ITcpServer, TCPServer>();
            services.AddSingleton<IUdpServer, UDPServer>();

            services.AddSingleton<IClientInfoCaching, ClientInfoCache>();

            services.AddSingleton<IServerRequest, ServerRequestHelper>();

            services.AddSingleton<ResetMessageHelper>();
            services.AddSingleton<HeartMessageHelper>();
            services.AddSingleton<ClientsHelper>();
            services.AddSingleton<ClientsMessageHelper>();
            services.AddSingleton<RegisterMessageHelper>();
            services.AddSingleton<RegisterHelper>();
            services.AddSingleton<RegisterState>();
            services.AddSingleton<ITunnelRegister, TunnelRegister>();
            

            services.AddSingleton<ServerPluginHelper>();

            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray(), typeof(IPlugin)))
            {
                services.AddSingleton(item);
            }
            return services;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<ClientsHelper>();

            ServerPluginHelper serverPluginHelper = services.GetService<ServerPluginHelper>();

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray(), typeof(IPlugin)))
            {
                serverPluginHelper.LoadPlugin(item, services.GetService(item));
            }
            return services;
        }
    }
}
