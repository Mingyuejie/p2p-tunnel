using common.extends;
using server;
using server.model;

namespace client.service.messengers.clients
{
    /// <summary>
    /// 服务器发来的客户端列表
    /// </summary>
    public class ClientsMessenger : IMessenger
    {
        private readonly ClientsMessengerSender clientsMessageHelper;
        public ClientsMessenger(ClientsMessengerSender clientsMessageHelper)
        {
            this.clientsMessageHelper = clientsMessageHelper;
        }

        public void Execute(IConnection connection)
        {
            ClientsInfo res = connection.ReceiveRequestWrap.Memory.DeBytes<ClientsInfo>();
            clientsMessageHelper.OnServerClientsData.Push(res);
        }
    }
}
