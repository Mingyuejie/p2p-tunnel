using client.service.clientService;
using client.service.punchHolePlugins;
using client.service.punchHolePlugins.plugins.tcp;
using client.service.serverPlugins.clients;
using common.extends;
using System.Collections.Generic;

namespace client.service.serverPlugins.clients.client
{
    public class ClientsPlugin : IClientServicePlugin
    {
        private readonly ClientsHelper clientsHelper;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly PunchHoleEventHandles punchHoldEventHandles;

        public ClientsPlugin(ClientsHelper clientsHelper, IPunchHoleTcp punchHoleTcp, PunchHoleEventHandles punchHoldEventHandles)
        {
            this.clientsHelper = clientsHelper;
            this.punchHoleTcp = punchHoleTcp;
            this.punchHoldEventHandles = punchHoldEventHandles;
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
            punchHoleTcp.SendStep2Stop(model.ID);
        }

        public void Offline(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            clientsHelper.OfflineClient(model.ID);
        }

        public void ConnectReverse(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            punchHoldEventHandles.SendReverse(model.ID);
        }
    }

    public class ConnectModel
    {
        public long ID { get; set; } = 0;
    }
}
