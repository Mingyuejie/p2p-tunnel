using client.servers.clientServer;
using common.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardClientConfigure: IClientConfigure
    {
        private Config  config;
        public TcpForwardClientConfigure( Config config)
        {
            this.config = config;
        }

        public string Name => "TCP转发";

        public string Author => "snltty";

        public string Desc => "白名单不为空时只允许白名单内端口";

        public bool Enable => config.Enable;

        public async Task<object> Load()
        {
            return await Task.FromResult(config);
        }

        public async Task<string> Save(string jsonStr)
        {
            config = jsonStr.DeJson<Config>();
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
