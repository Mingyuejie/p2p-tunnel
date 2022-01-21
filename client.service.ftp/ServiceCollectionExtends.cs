using client.servers.clientServer;
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
using System.Threading.Tasks;

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
            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFtpServerPlugin)))
            {
                services.AddSingleton(item);
            }
            foreach (var item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFtpClientPlugin)))
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

    public class Config : SettingModelBase
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
        public int UploadNum { get; set; } = 10;
        public int ReadWriteBufferSize { get; set; } = 10*1024*1024;
        public int SendPacketSize { get; set; } = 32*1024;
        

        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("ftp-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();

            config.ServerRoot = ServerRoot;
            config.Password = Password;
            config.Enable = Enable;
            config.UploadNum = UploadNum;
            config.ReadWriteBufferSize = ReadWriteBufferSize;
            config.SendPacketSize = SendPacketSize;

            await ToFile(config, "ftp-appsettings.json");
        }
    }
}
