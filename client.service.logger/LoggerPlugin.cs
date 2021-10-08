using client.servers.clientServer;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.logger
{
    public class LoggerPlugin : IClientServicePlugin
    {
        public List<LoggerModel> Data { get; } = new List<LoggerModel>();

        public PageModel List(ClientServicePluginExcuteWrap arg)
        {
            PageParamModel model = arg.Content.DeJson<PageParamModel>();

            IEnumerable<LoggerModel> res = Data.OrderByDescending(c => c.Time);
            if (model.Type >= 0)
            {
                res = res.Where(c => c.Type == (LoggerTypes)model.Type);
            }

            return new PageModel
            {
                PageIndex = model.PageIndex,
                PageSize = model.PageSize,
                Count = Data.Count(),
                Data = res.Skip((model.PageIndex - 1) * model.PageSize).Take(model.PageSize)
            };
        }

        public void Clear(ClientServicePluginExcuteWrap arg)
        {
            Data.Clear();
        }
    }

    public class PageParamModel
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Type { get; set; } = -1;
    }
    public class PageModel
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int Count { get; set; } = 10;
        public IEnumerable<LoggerModel> Data { get; set; } = Array.Empty<LoggerModel>();
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddLoggerPlugin(this ServiceCollection obj)
        {
            Config config = Config.ReadConfig();
            obj.AddSingleton((e) => config);

            return obj;
        }

        public static ServiceProvider UseLoggerPlugin(this ServiceProvider obj)
        {
            LoggerPlugin plugin = obj.GetService<LoggerPlugin>();
            Config config = obj.GetService<Config>();
            Logger.Instance.OnLogger.Sub((model) =>
            {
                if (config.Enable)
                {
                    plugin.Data.Add(model);
                    if (plugin.Data.Count > config.MaxLength)
                    {
                        plugin.Data.RemoveAt(0);
                    }
                }
            });

            Logger.Instance.Info("日志收集插件已加载");
            return obj;
        }
    }

    public class LoggerSettingPlugin : IClientServiceSettingPlugin
    {
        private readonly Config config;
        public LoggerSettingPlugin(Config config)
        {
            this.config = config;
        }

        public string Name => "日志信息";

        public string Author => "snltty";

        public string Desc => "收集日志输出到前端";

        public bool Enable => config.Enable;

        public object LoadSetting()
        {
            return config;
        }

        public string SaveSetting(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();

            config.Enable = _config.Enable;
            config.MaxLength = _config.MaxLength;
            config.SaveConfig();

            return string.Empty;
        }

        public bool SwitchEnable(bool enable)
        {
            config.Enable = enable;
            config.SaveConfig();
            return true;
        }
    }

    public class Config
    {
        public bool Enable { get; set; } = false;
        public int MaxLength { get; set; } = 100;

        public static Config ReadConfig()
        {
            if (File.Exists("logger-appsettings.json"))
            {
                Config config = File.ReadAllText("logger-appsettings.json").DeJson<Config>();
                return config;
            }
            return new Config();
        }

        public void SaveConfig()
        {
            Config config = ReadConfig();
            config.Enable = Enable;
            File.WriteAllText("logger-appsettings.json", config.ToJson(), Encoding.UTF8);
        }
    }


}
