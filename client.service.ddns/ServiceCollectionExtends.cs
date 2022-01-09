﻿using client.servers.clientServer;
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
        public static ServiceCollection AddDdnsPlugin(this ServiceCollection obj)
        {
            Config config = Config.ReadConfig().Result;
            obj.AddSingleton((e) => config);

            obj.AddDdnsPlugin(AppDomain.CurrentDomain.GetAssemblies());

            return obj;
        }

        public static ServiceCollection AddDdnsPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IDdnsPlatform)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }

            return obj;
        }

        public static ServiceProvider UseDdnsPlugin(this ServiceProvider obj)
        {
            Logger.Instance.Info("ddns域名解析插件已加载");
            return obj;
        }
    }

    public class Config : SettingModelBase
    {
        public bool Enable { get; set; } = false;
        public int Interval { get; set; } = 100;
        public PlatformInfo[] Platforms { get; set; } = Array.Empty<PlatformInfo>();

        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("ddns-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();
            config.Platforms = Platforms;
            config.Enable = Enable;
            config.Interval = Interval;

            await ToFile(config, "ddns-appsettings.json");
        }
    }

    public class PlatformInfo
    {
        public string Name { get; set; } = string.Empty;
        public GroupInfo[] Groups { get; set; } = Array.Empty<GroupInfo>();

    }
    public class GroupInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public List<string> Records { get; set; } = new List<string>();
    }
}
