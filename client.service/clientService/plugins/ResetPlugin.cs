using client.service.serverPlugins.clients;
using client.service.serverPlugins.reset;
using common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.clientService.plugins
{
    public class ResetPlugin : IClientServicePlugin
    {
        public void Reset(ClientServicePluginExcuteWrap arg)
        {
            ResetModel model = Helper.DeJsonSerializer<ResetModel>(arg.Content);

            if (model.ID > 0)
            {
                if (AppShareData.Instance.Clients.TryGetValue(model.ID, out ClientInfo client))
                {
                    if (client != null)
                    {
                        ResetEventHandles.Instance.SendResetMessage(client.Socket, model.ID);
                    }
                }
            }
            arg.Callback(arg, null);
        }
    }

    public class ResetModel
    {
        public long ID { get; set; } = 0;
    }
}
