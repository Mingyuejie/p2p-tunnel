using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using client.service.plugins.punchHolePlugins;
using client.service.plugins.punchHolePlugins.plugins.tcp;
using client.service.servers.clientServer;
using common.extends;
using server.plugins.register.caching;
using System.Collections.Generic;
using System.Threading.Tasks;

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


        public IEnumerable<ClientInfo> List(ClientServicePluginExecuteWrap arg)
        {
            return clientInfoCaching.All();
        }

        public void Connect(ClientServicePluginExecuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            clientsHelper.ConnectClient(model.ID);
        }

        public void Stop(ClientServicePluginExecuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            punchHoleTcp.SendStep2Stop(model.ID);
        }

        public void Offline(ClientServicePluginExecuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            clientInfoCaching.Offline(model.ID);
        }

        public async Task ConnectReverse(ClientServicePluginExecuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            await punchHoldEventHandles.SendReverse(model.ID);
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
        public ulong ID { get; set; } = 0;
    }
}
