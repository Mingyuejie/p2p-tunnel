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
        

        public static ServerTcpForwardRegisterConfig ReadConfig()
        {
            return FromFile<ServerTcpForwardRegisterConfig>("servertcpforward-appsettings.json") ?? new ServerTcpForwardRegisterConfig();
        }

        public void SaveConfig()
        {
            ServerTcpForwardRegisterConfig config = ReadConfig();
            config.Web = Web;
            config.Tunnel = Tunnel;
            ToFile(config, "servertcpforward-appsettings.json");
        }
    }
}
