using client.servers.clientServer;
using client.service.serverTcpforward;
using common.extends;
using System;
using System.Collections.Generic;
using System.IO;

namespace client.service.tcpforward.client
{
    public class ServerTcpForwardPlugin : IClientServicePlugin
    {
        private readonly ServerTcpForwardHelper serverTcpForwardHelper;
        public ServerTcpForwardPlugin(ServerTcpForwardHelper serverTcpForwardHelper)
        {
            this.serverTcpForwardHelper = serverTcpForwardHelper;
        }

        public void Register(ClientServicePluginExcuteWrap arg)
        {
            string msg = serverTcpForwardHelper.Register();

            if (!string.IsNullOrWhiteSpace(msg))
            {
                arg.SetCode(-1, msg);
            }
        }
    }
}
