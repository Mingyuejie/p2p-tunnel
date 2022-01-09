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
        public static ServiceCollection AddCmdPlugin(this ServiceCollection obj)
        {
            Config config = Config.ReadConfig().Result;
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

        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("cmd-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();
            config.Password = Password;
            config.Enable = Enable;

            await ToFile(config, "cmd-appsettings.json");
        }
    }
}
