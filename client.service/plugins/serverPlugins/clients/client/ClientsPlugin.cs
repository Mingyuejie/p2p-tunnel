using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using client.service.plugins.punchHolePlugins;
using client.service.plugins.punchHolePlugins.plugins.tcp;
using client.service.servers.clientServer;
using common.extends;
using server.plugins.register.caching;
using System.Collections.Generic;

namespace client.service.plugins.serverPlugins.clients.client
{
    public class ClientsPlugin : IClientServicePlugin
    {
        private readonly ClientsHelper clientsHelper;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        private readonly IClientInfoCaching clientInfoCaching;

        public ClientsPlugin(ClientsHelper clientsHelper, IPunchHoleTcp punchHoleTcp,
            IClientInfoCaching clientInfoCaching,
            PunchHoleEventHandles punchHoldEventHandles)
        {
            this.clientsHelper = clientsHelper;
            this.punchHoleTcp = punchHoleTcp;
            this.clientInfoCaching = clientInfoCaching;
            this.punchHoldEventHandles = punchHoldEventHandles;
        }


        public IEnumerable<ClientInfo> List(ClientServicePluginExcuteWrap arg)
        {
            return clientInfoCaching.All();
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

    public class ClientsPushMsgPlugin : IClientServerPushMsgPlugin
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public ClientsPushMsgPlugin(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        public IEnumerable<ClientInfo> List()
        {
            return clientInfoCaching.All();
        }
    }

    public class ConnectModel
    {
        public long ID { get; set; } = 0;
    }
}
