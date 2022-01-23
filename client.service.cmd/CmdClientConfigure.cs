using client.servers.clientServer;
using common.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.cmd
{
    public class CmdClientConfigure : IClientConfigure
    {
        private readonly Config config;
        public CmdClientConfigure(Config config)
        {
            this.config = config;
        }

        public string Name => "远程命令";

        public string Author => "snltty";

        public string Desc => "执行远程客户端的命令行";

        public bool Enable => config.Enable;

        public async Task<object> Load()
        {
            return await Task.FromResult(config);
        }

        public async Task<string> Save(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();
            config.Password = _config.Password;
            config.Enable = _config.Enable;
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
