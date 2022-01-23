using client.servers.clientServer;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverTcpforward
{
    public class Config : ClientConfigureInfoBase
    {
        public ServerTcpForwardWebInfo[] Web { get; set; } = Array.Empty<ServerTcpForwardWebInfo>();
        public ServerTcpForwardTunnelInfo[] Tunnel { get; set; } = Array.Empty<ServerTcpForwardTunnelInfo>();

        public bool Enable { get; set; } = true;
        public bool AutoReg { get; set; } = true;


        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("servertcpforward-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();
            config.Web = Web;
            config.Tunnel = Tunnel;
            await ToFile(config, "servertcpforward-appsettings.json");
        }
    }
}
