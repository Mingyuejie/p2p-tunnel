using client.servers.clientServer;
using common.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.logger
{
    public class LoggerClientConfigure : IClientConfigure
    {
        private readonly Config config;
        public LoggerClientConfigure(Config config)
        {
            this.config = config;
        }

        public string Name => "日志信息";

        public string Author => "snltty";

        public string Desc => "收集日志输出到前端";

        public bool Enable => config.Enable;

        public async Task<object> Load()
        {
            return await Task.FromResult(config);
        }

        public async Task<string> Save(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();

            config.Enable = _config.Enable;
            config.MaxLength = _config.MaxLength;
            await config.SaveConfig();

            return string.Empty;
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            config.Enable = enable;
            await config.SaveConfig();
            return true;
        }
    }

}
