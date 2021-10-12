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
            var res = serverTcpForwardHelper.Register();

            if (res.Code != server.model.ServerMessageResponeCodes.OK)
            {
                arg.SetCode(-1, res.ErrorMsg);
            }
        }
    }
}
