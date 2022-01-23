using client.servers.clientServer;
using client.service.serverTcpforward;
using common.extends;
using server;
using server.model;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class ServerTcpForwardClientService : IClientService
    {
        private readonly ServerTcpForwardHelper serverTcpForwardHelper;
        public ServerTcpForwardClientService(ServerTcpForwardHelper serverTcpForwardHelper)
        {
            this.serverTcpForwardHelper = serverTcpForwardHelper;
        }

        public async Task<bool> Register(ClientServiceParamsInfo arg)
        {
            MessageResponeInfo res = await serverTcpForwardHelper.Register();
            ServerTcpForwardRegisterResponseCodes code = res.Data.DeBytes<ServerTcpForwardRegisterResponseCodes>();
            if(code != ServerTcpForwardRegisterResponseCodes.OK)
            {
                arg.SetErrorMessage(code.GetDesc((byte)code));
            }
            return true;
        }
    }
}
