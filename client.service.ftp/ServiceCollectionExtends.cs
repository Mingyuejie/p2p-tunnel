using client.service.ftp.client;
using client.service.ftp.plugin;
using client.service.ftp.server;
using client.service.ftp.server.plugin;
using common;
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
            obj.AddSingleton((e) => config);

            obj.AddFtpPlugin(AppDomain.CurrentDomain.GetAssemblies());
            obj.AddSingleton<FtpServer>();
            obj.AddSingleton<FtpClient>();
            return obj;
        }
        public static ServiceCollection AddFtpPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IFtpServerPlugin)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }
            var types2 = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IFtpClientPlugin)));
            foreach (var item in types2)
            {
                obj.AddSingleton(item);
            }

            return obj;
        }

        public static ServiceProvider UseFtpPlugin(this ServiceProvider obj)
        {
            obj.UseFtpPlugin(AppDomain.CurrentDomain.GetAssemblies());

            Logger.Instance.Info("文件服务插件已加载");
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
        private string serverRoot = string.Empty;
        public string ServerRoot
        {
            get
            {
                return serverRoot;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    serverRoot = Directory.GetCurrentDirectory();
                }
                else
                {
                    serverRoot = new DirectoryInfo(value).FullName;
                }
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string ServerCurrentPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string ClientRootPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string ClientCurrentPath { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool Enable { get; set; } = false;


        public static Config ReadConfig()
        {
            Config config = File.ReadAllText("ftp-appsettings.json").DeJson<Config>();
            return config;
        }

        public void SaveConfig()
        {
            Config config = File.ReadAllText("ftp-appsettings.json").DeJson<Config>();

            config.ServerRoot = ServerRoot;
            config.Password = Password;
            config.Enable = Enable;

            File.WriteAllText("ftp-appsettings.json", config.ToJson(), System.Text.Encoding.UTF8);
        }
    }
}
