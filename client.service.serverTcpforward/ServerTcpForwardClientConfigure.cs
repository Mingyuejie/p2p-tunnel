using client.servers.clientServer;
using common.extends;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverTcpforward
{
    public class ServerTcpForwardClientConfigure : IClientConfigure
    {
        private readonly Config serverTcpForwardRegisterConfig;
        private readonly ServerTcpForwardHelper serverTcpForwardHelper;
        public ServerTcpForwardClientConfigure(Config serverTcpForwardRegisterConfig, ServerTcpForwardHelper serverTcpForwardHelper)
        {
            this.serverTcpForwardRegisterConfig = serverTcpForwardRegisterConfig;
            this.serverTcpForwardHelper = serverTcpForwardHelper;
        }

        public string Name => "服务器TCP代理";

        public string Author => "snltty";

        public string Desc => "不打洞，直接服务器TCP代理";

        public bool Enable => serverTcpForwardRegisterConfig.Enable;

        public async Task<object> Load()
        {
            return await Task.FromResult(serverTcpForwardRegisterConfig);
        }

        public async Task<string> Save(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();

            serverTcpForwardRegisterConfig.Enable = _config.Enable;
            serverTcpForwardRegisterConfig.Web = _config.Web;
            serverTcpForwardRegisterConfig.Tunnel = _config.Tunnel;
            await serverTcpForwardRegisterConfig.SaveConfig();

            serverTcpForwardHelper.UnRegister().Wait();
            var res = serverTcpForwardHelper.Register().Result;
            if (res.Code != MessageResponeCodes.OK)
            {
                return res.Code.GetDesc((byte)res.Code);
            }

            return string.Empty;
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            serverTcpForwardRegisterConfig.Enable = enable;
            await serverTcpForwardRegisterConfig.SaveConfig();

            serverTcpForwardHelper.UnRegister().Wait();
            serverTcpForwardHelper.Register().Wait();

            return true;
        }
    }

}
