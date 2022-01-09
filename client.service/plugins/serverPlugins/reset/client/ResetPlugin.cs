using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using common.extends;
using server.plugins.register.caching;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.reset.client
{
    public class ResetPlugin : IClientServicePlugin
    {
        private readonly ResetMessageHelper resetEventHandles;
        private readonly IClientInfoCaching clientInfoCaching;

        public ResetPlugin(IClientInfoCaching clientInfoCaching, ResetMessageHelper resetEventHandles)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.resetEventHandles = resetEventHandles;
        }

        public async Task<bool> Reset(ClientServicePluginExecuteWrap arg)
        {
            ResetModel model = arg.Content.DeJson<ResetModel>();

            if (model.ID > 0)
            {
                if (clientInfoCaching.Get(model.ID, out ClientInfo client))
                {
                    return (await resetEventHandles.SendResetMessage(client.TcpConnection, model.ID)).Code == server.model.MessageResponeCode.OK;
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
