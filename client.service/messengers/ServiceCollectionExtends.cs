using cclient.service.messengers.reset;
using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.service.messengers.clients;
using client.service.messengers.heart;
using client.service.messengers.punchHole;
using client.service.messengers.punchHole.tcp.nutssb;
using client.service.messengers.punchHole.udp;
using client.service.messengers.register;
using common;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.servers.defaults;
using System;
using System.Linq;
using System.Reflection;

namespace client.service.plugins.serverPlugins
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddServerPlugin(this ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<ITcpServer, TCPServer>();
            services.AddSingleton<IUdpServer, UDPServer>();

            services.AddSingleton<IClientInfoCaching, ClientInfoCaching>();

            services.AddSingleton<ResetMessengerSender>();
            services.AddSingleton<HeartMessengerSender>();
            services.AddSingleton<ClientsHelper>();
            services.AddSingleton<ClientsMessengerSender>();
            services.AddSingleton<RegisterMessengerSender>();
            services.AddSingleton<RegisterHelper>();
            services.AddSingleton<RegisterStateInfo>();
            services.AddSingleton<IDataTunnelRegister, DataTunnelRegister>();


            services.AddSingleton<PunchHoleMessengerSender>();
            services.AddSingleton<IPunchHoleUdp, PunchHoleUdpMessengerSender>();
            services.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssBMessengerSender>();

            services.AddSingleton<MessengerResolver>();
            services.AddSingleton<MessengerSender>();


            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray(), typeof(IMessenger)))
            {
                services.AddSingleton(item);
            }

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray(), typeof(IPunchHole)))
            {
                services.AddSingleton(item);
            }

            return services;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<ClientsHelper>();
            services.GetService<MessengerSender>();
            MessengerResolver serverPluginHelper = services.GetService<MessengerResolver>();

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray(), typeof(IMessenger)))
            {
                serverPluginHelper.LoadMessenger(item, services.GetService(item));
            }

            services.GetService<PunchHoleMessengerSender>().LoadPlugins(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray());

            return services;
        }
    }
}
