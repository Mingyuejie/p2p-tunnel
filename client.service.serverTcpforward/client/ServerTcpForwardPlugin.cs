using client.servers.clientServer;
using client.service.serverTcpforward;
using common.extends;
using server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace client.service.tcpforward.client
{
    public class ServerTcpForwardPlugin : IClientServicePlugin
    {
        private readonly ServerTcpForwardHelper serverTcpForwardHelper;
        public ServerTcpForwardPlugin(ServerTcpForwardHelper serverTcpForwardHelper)
        {
            this.serverTcpForwardHelper = serverTcpForwardHelper;
        }

        public async Task<bool> Register(ClientServicePluginExecuteWrap arg)
        {
            MessageRequestResponeWrap res = await serverTcpForwardHelper.Register();
            ServerTcpForwardRegisterResponseCode code = res.Data.DeBytes<ServerTcpForwardRegisterResponseCode>();
            if(code != ServerTcpForwardRegisterResponseCode.OK)
            {
                arg.SetErrorMessage(code.GetDesc((byte)code));
            }
            return true;
        }
    }
}
