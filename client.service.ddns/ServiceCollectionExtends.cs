using client.servers.clientServer;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace client.service.ddns
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddDdnsPlugin(this ServiceCollection obj)
        {
            Config config = Config.ReadConfig();
            obj.AddSingleton((e) => config);

            return obj;
        }
        public static ServiceCollection AddDdnsPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {

            return obj;
        }

        public static ServiceProvider UseDdnsPlugin(this ServiceProvider obj)
        {
            Logger.Instance.Info("ddns域名解析插件已加载");
            return obj;
        }

        public static ServiceProvider UseDdnsPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            return obj;
        }
    }

    public class Config : SettingModelBase
    {
        public PlatformInfo Platform { get; set; } = new PlatformInfo();

        public bool Enable { get; set; } = false;

        public static Config ReadConfig()
        {
            return FromFile<Config>("ddns-appsettings.json") ?? new Config();
        }

        public void SaveConfig()
        {
            Config config = ReadConfig();
            config.Platform = Platform;
            config.Enable = Enable;

            ToFile(config, "ddns-appsettings.json");
        }
    }

    public class PlatformInfo
    {
        public string Name { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string AccessSecret { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
    }
}
