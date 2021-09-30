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

    public class Config
    {
        private string root = string.Empty;
        public string Root
        {
            get
            {
                return root;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    root = Directory.GetCurrentDirectory();
                }
                else
                {
                    root = new DirectoryInfo(value).FullName;
                }
            }
        }

        public string Password { get; set; } = string.Empty;

        public bool Enable { get; set; } = false;



        public static Config ReadConfig()
        {
            if (File.Exists("cmd-appsettings.json"))
            {
                Config config = File.ReadAllText("cmd-appsettings.json").DeJson<Config>();
                return config;
            }
            return new Config();
        }

        public void SaveConfig()
        {
            Config config = ReadConfig();
            config.Root = Root;
            config.Password = Password;
            config.Enable = Enable;
            File.WriteAllText("cmd-appsettings.json", config.ToJson(), System.Text.Encoding.UTF8);
        }
    }
}
