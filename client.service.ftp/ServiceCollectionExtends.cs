using client.service.ftp.client;
using client.service.ftp.server;
using common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace client.service.ftp
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddFtpPlugin(this ServiceCollection services)
        {
            Config config = Config.ReadConfig().Result;
            services.AddSingleton((e) => config);

            services.AddFtpPlugin(AppDomain.CurrentDomain.GetAssemblies());
            services.AddSingleton<FtpServer>();
            services.AddSingleton<FtpClient>();
            return services;
        }
        public static ServiceCollection AddFtpPlugin(this ServiceCollection services, Assembly[] assemblys)
        {
            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFtpCommandServerPlugin)))
            {
                services.AddSingleton(item);
            }
            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFtpCommandClientPlugin)))
            {
                services.AddSingleton(item);
            }

            return services;
        }

        public static ServiceProvider UseFtpPlugin(this ServiceProvider services)
        {
            services.UseFtpPlugin(AppDomain.CurrentDomain.GetAssemblies());

            Logger.Instance.Info("文件服务插件已加载");

            return services;
        }

        public static ServiceProvider UseFtpPlugin(this ServiceProvider services, Assembly[] assemblys)
        {
            services.GetService<FtpServer>().LoadPlugins(assemblys);
            services.GetService<FtpClient>().LoadPlugins(assemblys);

            return services;
        }
    }

   
}
