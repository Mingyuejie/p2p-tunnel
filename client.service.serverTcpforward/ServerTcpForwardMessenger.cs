using common.extends;
using server;
using server.model;
using System.Threading.Tasks;

namespace client.service.serverTcpforward
{
    /// <summary>
    /// 服务器TCP代理转发
    /// </summary>
    public class ServerTcpForwardMessenger : IMessenger
    {
        private readonly ServerTcpForwardHelper serverTcpForwardHelper;
        public ServerTcpForwardMessenger(ServerTcpForwardHelper serverTcpForwardHelper)
        {
            this.serverTcpForwardHelper = serverTcpForwardHelper;
        }

        public async Task Execute(IConnection connection)
        {
            ServerTcpForwardInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<ServerTcpForwardInfo>();
            await serverTcpForwardHelper.Request(model);
        }
    }

}
