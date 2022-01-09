using client.servers.clientServer;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverTcpforward
{
    public class ServerTcpForwardRegisterConfig : SettingModelBase
    {
        public ServerForwardWebModel[] Web { get; set; } = Array.Empty<ServerForwardWebModel>();
        public ServerTcpForwardTunnelModel[] Tunnel { get; set; } = Array.Empty<ServerTcpForwardTunnelModel>();

        public bool Enable { get; set; } = true;
        public bool AutoReg { get; set; } = true;


        public static async Task<ServerTcpForwardRegisterConfig> ReadConfig()
        {
            return await FromFile<ServerTcpForwardRegisterConfig>("servertcpforward-appsettings.json") ?? new ServerTcpForwardRegisterConfig();
        }

        public async Task SaveConfig()
        {
            ServerTcpForwardRegisterConfig config = await ReadConfig();
            config.Web = Web;
            config.Tunnel = Tunnel;
            await ToFile(config, "servertcpforward-appsettings.json");
        }
    }
}
