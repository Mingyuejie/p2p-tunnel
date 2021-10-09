﻿using client.servers.clientServer;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace client.service.cmd
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddCmdPlugin(this ServiceCollection obj)
        {
            Config config = Config.ReadConfig();
            obj.AddSingleton((e) => config);

            return obj;
        }
        public static ServiceCollection AddCmdPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
           
            return obj;
        }

        public static ServiceProvider UseCmdPlugin(this ServiceProvider obj)
        {
            Logger.Instance.Info("远程命令插件已加载");
            return obj;
        }

        public static ServiceProvider UseCmdPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            return obj;
        }
    }

    public class Config: SettingModelBase
    {
        public string Password { get; set; } = string.Empty;
        public bool Enable { get; set; } = false;

        public static Config ReadConfig()
        {
            return FromFile<Config>("cmd-appsettings.json") ?? new Config();
        }

        public void SaveConfig()
        {
            Config config = ReadConfig();
            config.Password = Password;
            config.Enable = Enable;

            ToFile(config, "cmd-appsettings.json");
        }
    }
}
