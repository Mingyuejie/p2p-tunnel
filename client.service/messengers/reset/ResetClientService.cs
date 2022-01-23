using cclient.service.messengers.reset;
using client.messengers.clients;
using client.servers.clientServer;
using common.extends;
using System.Threading.Tasks;

namespace client.service.messengers.reset
{
    public class ResetClientService : IClientService
    {
        private readonly ResetMessengerSender resetEventHandles;
        private readonly IClientInfoCaching clientInfoCaching;

        public ResetClientService(IClientInfoCaching clientInfoCaching, ResetMessengerSender resetEventHandles)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.resetEventHandles = resetEventHandles;
        }

        public async Task<bool> Reset(ClientServiceParamsInfo arg)
        {
            ResetModel model = arg.Content.DeJson<ResetModel>();

            if (model.ID > 0)
            {
                if (clientInfoCaching.Get(model.ID, out ClientInfo client))
                {
                    return (await resetEventHandles.SendResetMessage(client.TcpConnection, model.ID)).Code == server.model.MessageResponeCodes.OK;
                }
            }
            return false;
        }
    }

    public class ResetModel
    {
        public ulong ID { get; set; } = 0;
    }
}
