using common.extends;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.clients
{
    /// <summary>
    /// 服务器发来的客户端列表
    /// </summary>
    public class ClientsPlugin : IPlugin
    {
        private readonly ClientsEventHandles clientsEventHandles;
        public ClientsPlugin(ClientsEventHandles clientsEventHandles) {
            this.clientsEventHandles = clientsEventHandles;
        }

        public void Excute(PluginExcuteModel model)
        {
            ClientsModel res = model.Wrap.Content.DeBytes<ClientsModel>();
            clientsEventHandles.OnServerSendClients(new OnServerSendClientsEventArg
            {
                Data = res,
                Packet = model
            });
        }
    }
}
