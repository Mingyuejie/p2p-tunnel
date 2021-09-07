using common.extends;
using server.model;
using server.plugin;

namespace client.service.serverPlugins.clients
{
    /// <summary>
    /// 服务器发来的客户端列表
    /// </summary>
    public class ServerSendClientsPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_SEND_CLIENTS;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            ClientsModel res = model.Packet.Chunk.DeBytes<ClientsModel>();

            if (serverType == ServerType.UDP)
            {
                ClientsEventHandles.Instance.OnServerSendClients(new OnServerSendClientsEventArg
                {
                    Data = res,
                    Packet = model
                });
            }
        }
    }
}
