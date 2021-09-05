using client.service.serverPlugins.clients;
using client.service.serverPlugins.connectClient;
using common;
using System.Collections.Generic;
using System.Linq;

namespace client.service.clientService.plugins
{
    public class ClientsPlugin : IClientServicePlugin
    {
        public void List(ClientServicePluginExcuteWrap arg)
        {
            arg.Callback(arg, AppShareData.Instance.Clients.Values.ToList());
        }

        public void Connect(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = Helper.DeJsonSerializer<ConnectModel>(arg.Content);
            ClientsHelper.Instance.ConnectClient(model.ID);

            arg.Callback(arg, null);
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = Helper.DeJsonSerializer<ConnectModel>(arg.Content);
            ConnectClientEventHandles.Instance.SendTcpConnectClientStep2StopMessage(model.ID);
            arg.Callback(arg, null);
        }

        public void Offline(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = Helper.DeJsonSerializer<ConnectModel>(arg.Content);
            ClientsHelper.Instance.OfflineClient(model.ID);
            arg.Callback(arg, null);
        }

        public void ConnectReverse(ClientServicePluginExcuteWrap arg)
        {
            ConnectModel model = Helper.DeJsonSerializer<ConnectModel>(arg.Content);
            ConnectClientEventHandles.Instance.SendConnectClientReverseMessage(model.ID);
            arg.Callback(arg, null);
        }
    }

    public class ConnectModel
    {
        public long ID { get; set; } = 0;
    }
}
