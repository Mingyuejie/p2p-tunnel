using client.service.clientService;
using client.service.config;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.reset;
using common;
using common.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverPlugins.reset.client
{
    public class ResetPlugin : IClientServicePlugin
    {
        private readonly ResetEventHandles  resetEventHandles;
        private readonly ClientsHelper   clientsHelper;
        private readonly Config config;
        public ResetPlugin(ClientsHelper clientsHelper, Config config, ResetEventHandles resetEventHandles)
        {
            this.clientsHelper = clientsHelper;
            this.config = config;
            this.resetEventHandles = resetEventHandles;
        }

        public void Reset(ClientServicePluginExcuteWrap arg)
        {
            ResetModel model = arg.Content.DeJson<ResetModel>();

            if (model.ID > 0)
            {
                if (clientsHelper.Get(model.ID, out ClientInfo client))
                {
                    if (client != null)
                    {
                        resetEventHandles.SendResetMessage(client.Socket, model.ID);
                    }
                }
            }
        }
    }

    public class ResetModel
    {
        public long ID { get; set; } = 0;
    }
}
