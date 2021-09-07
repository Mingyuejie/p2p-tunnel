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
        public IEnumerable<ClientInfo> List(ClientServicePluginExcuteWrap arg)
        {
            return AppShareData.Instance.Clients.Values.ToList();
        }

        public void Connect(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            ClientsHelper.Instance.ConnectClient(model.ID);
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            ConnectClientEventHandles.Instance.SendTcpConnectClientStep2StopMessage(model.ID);
        }

        public void Offline(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            ClientsHelper.Instance.OfflineClient(model.ID);
        }

        public void ConnectReverse(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = arg.Content.DeJson<ConnectModel>();
            ConnectClientEventHandles.Instance.SendConnectClientReverse(model.ID);
        }
    }

    public class ConnectModel
    {
        public long ID { get; set; } = 0;
    }
}
