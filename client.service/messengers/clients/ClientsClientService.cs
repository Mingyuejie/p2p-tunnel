using client.messengers.clients;
using client.messengers.punchHole.tcp;
using client.servers.clientServer;
using client.service.messengers.punchHole;
using common.extends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.messengers.clients
{
    public class ClientsClientService : IClientService
    {
        private readonly ClientsHelper clientsHelper;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly PunchHoleMessengerSender punchHoldEventHandles;
        private readonly IClientInfoCaching clientInfoCaching;

        public ClientsClientService(ClientsHelper clientsHelper, IPunchHoleTcp punchHoleTcp,
            IClientInfoCaching clientInfoCaching,
            PunchHoleMessengerSender punchHoldEventHandles)
        {
            this.clientsHelper = clientsHelper;
            this.punchHoleTcp = punchHoleTcp;
            this.clientInfoCaching = clientInfoCaching;
            this.punchHoldEventHandles = punchHoldEventHandles;
        }


        public IEnumerable<ClientInfo> List(ClientServiceParamsInfo arg)
        {
            return clientInfoCaching.All();
        }

        public void Connect(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientsHelper.ConnectClient(model.ID);
        }

        public void Stop(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            punchHoleTcp.SendStep2Stop(model.ID);
        }

        public void Offline(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            clientInfoCaching.Offline(model.ID);
        }

        public async Task ConnectReverse(ClientServiceParamsInfo arg)
        {
            ConnectParamsInfo model = arg.Content.DeJson<ConnectParamsInfo>();
            await punchHoldEventHandles.SendReverse(model.ID);
        }
    }

    public class ConnectParamsInfo
    {
        public ulong ID { get; set; } = 0;
    }
}
