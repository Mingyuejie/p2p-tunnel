using client.servers.clientServer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace client.service.servers.clientServer
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddClientServer(this ServiceCollection services, Assembly[] assemblys)
        {
            services.AddSingleton<IClientServer, ClientServer>();

            IEnumerable<Type> types = assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).SelectMany(c => c.GetTypes());

            foreach (var item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientService))))
            {
                services.AddSingleton(item);
            }
            foreach (var item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientPushMsg))))
            {
                services.AddSingleton(item);
            }
            foreach (var item in types.Where(c => c.GetInterfaces().Contains(typeof(IClientConfigure))))
            {
                services.AddSingleton(item);
            }

            return services;
        }

        public static ServiceProvider UseClientServer(this ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<IClientServer>().Start();
            services.GetService<IClientServer>().LoadPlugins(assemblys.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray());
            return services;
        }
    }
}
