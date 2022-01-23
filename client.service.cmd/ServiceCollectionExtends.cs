using client.servers.clientServer;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.cmd
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddCmdPlugin(this ServiceCollection services)
        {
            Config config = Config.ReadConfig().Result;
            services.AddSingleton((e) => config);

            return services;
        }
        public static ServiceCollection AddCmdPlugin(this ServiceCollection services, Assembly[] assemblys)
        {
           
            return services;
        }

        public static ServiceProvider UseCmdPlugin(this ServiceProvider services)
        {
            Logger.Instance.Info("远程命令插件已加载");
            return services;
        }

        public static ServiceProvider UseCmdPlugin(this ServiceProvider services, Assembly[] assemblys)
        {
            return services;
        }
    }

    
}
