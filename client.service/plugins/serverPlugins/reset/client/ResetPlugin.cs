using client.service.plugins.serverPlugins.clients;
using client.service.servers.clientServer;
using common.extends;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.reset.client
{
    public class ResetPlugin : IClientServicePlugin
    {
        private readonly ResetEventHandles resetEventHandles;
        private readonly ClientsHelper clientsHelper;
        public ResetPlugin(ClientsHelper clientsHelper, ResetEventHandles resetEventHandles)
        {
            this.clientsHelper = clientsHelper;
            this.resetEventHandles = resetEventHandles;
        }

        public async Task<bool> Reset(ClientServicePluginExcuteWrap arg)
        {
            ResetModel model = arg.Content.DeJson<ResetModel>();

            if (model.ID > 0)
            {
                if (clientsHelper.Get(model.ID, out ClientInfo client))
                {
                    if (client != null)
                    {
                        await resetEventHandles.SendResetMessage(client.Socket, model.ID);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class ResetModel
    {
        public long ID { get; set; } = 0;
    }
}
