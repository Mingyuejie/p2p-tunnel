using common;
using server.model;
using System;

namespace client.service.plugins.serverPlugins.clients
{
    public class ClientsMessageHelper
    {
        public ClientsMessageHelper()
        {
        }

        public SimplePushSubHandler<OnServerSendClientsEventArg> OnServerClientsData { get; } = new SimplePushSubHandler<OnServerSendClientsEventArg>();

    }

    public class OnServerSendClientsEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public ClientsModel Data { get; set; }
    }

}
