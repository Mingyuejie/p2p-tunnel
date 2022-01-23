using client.servers.clientServer;
using client.service.ddns.platform;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.ddns
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddDdnsPlugin(this ServiceCollection services)
        {
            Config config = Config.ReadConfig().Result;
            services.AddSingleton((e) => config);

            services.AddDdnsPlugin(AppDomain.CurrentDomain.GetAssemblies());

            return services;
        }

        public static ServiceCollection AddDdnsPlugin(this ServiceCollection services, Assembly[] assemblys)
        {
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IDdnsPlatform)))
            {
                services.AddSingleton(item);
            }

            return services;
        }

        public static ServiceProvider UseDdnsPlugin(this ServiceProvider services)
        {
            Logger.Instance.Info("ddns域名解析插件已加载");
            return services;
        }
    }

    
}
