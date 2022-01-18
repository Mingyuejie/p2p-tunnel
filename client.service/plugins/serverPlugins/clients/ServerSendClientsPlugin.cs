using common.extends;
using server;
using server.model;
using server.plugin;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.clients
{
    /// <summary>
    /// 服务器发来的客户端列表
    /// </summary>
    public class ClientsPlugin : IPlugin
    {
        private readonly ClientsMessageHelper clientsMessageHelper;
        public ClientsPlugin(ClientsMessageHelper clientsMessageHelper)
        {
            this.clientsMessageHelper = clientsMessageHelper;
        }

        public void Execute(IConnection connection)
        {
            ClientsModel res = connection.ReceiveRequestWrap.Memory.DeBytes<ClientsModel>();
            clientsMessageHelper.OnServerClientsData.Push(res);
        }
    }
}
