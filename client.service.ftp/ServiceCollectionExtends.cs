using client.service.ftp.client;
using client.service.ftp.plugin;
using client.service.ftp.server;
using client.service.ftp.server.plugin;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace client.service.ftp
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddFtpPlugin(this ServiceCollection obj)
        {
            Config config = Config.ReadConfig();
            obj.AddSingleton((e)=> config);

            obj.AddFtpPlugin(AppDomain.CurrentDomain.GetAssemblies());
            obj.AddSingleton<FtpServer>();
            obj.AddSingleton<FtpClient>();
            return obj;
        }
        public static ServiceCollection AddFtpPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IFtpPlugin)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }
            return obj;
        }

        public static ServiceProvider UseFtpPlugin(this ServiceProvider obj)
        {
            obj.UseFtpPlugin(AppDomain.CurrentDomain.GetAssemblies());
            return obj;
        }

        public static ServiceProvider UseFtpPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            obj.GetService<FtpServer>().LoadPlugins(assemblys);
            obj.GetService<FtpClient>().LoadPlugins(assemblys);
            return obj;
        }
    }

    public class Config
    {
        public string ServerRoot { get; set; } = string.Empty;
        public string ServerCurrentPath { get; set; } = string.Empty;
        public string ClientCurrentPath { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
       

        public static Config ReadConfig()
        {
            Config config = File.ReadAllText("album-appsettings.json").DeJson<Config>();
            return config;
        }

        public void SaveConfig()
        {
            Config config = File.ReadAllText("album-appsettings.json").DeJson<Config>();

            config.ServerRoot = ServerRoot;
            config.Password = Password;

            File.WriteAllText("album-appsettings.json", config.ToJson(), System.Text.Encoding.UTF8);
        }
    }
}
