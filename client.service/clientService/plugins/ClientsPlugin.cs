using client.service.serverPlugins.clients;
using client.service.serverPlugins.connectClient;
using common;
using common.extends;
using System.Collections.Generic;
using System.Linq;

namespace client.service.clientService.plugins
{
    public class ClientsPlugin : IClientServicePlugin
    {
        private readonly ClientsHelper clientsHelper;
        private readonly ConnectClientEventHandles  connectClientEventHandles;
        
        public ClientsPlugin(ClientsHelper clientsHelper, ConnectClientEventHandles connectClientEventHandles)
        {
            this.clientsHelper = clientsHelper;
            this.connectClientEventHandles = connectClientEventHandles;
        }


        public IEnumerable<ClientInfo> List(ClientServicePluginExcuteWrap arg)
        {
            return clientsHelper.Clients;
        }

        public void Connect(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            clientsHelper.ConnectClient(model.ID);
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            connectClientEventHandles.SendTcpConnectClientStep2StopMessage(model.ID);
        }

        public void Offline(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            clientsHelper.OfflineClient(model.ID);
        }

        public void ConnectReverse(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            connectClientEventHandles.SendConnectClientReverse(model.ID);
        }
    }

    public class ConnectModel
    {
        public long ID { get; set; } = 0;
    }
}
