using common;
using server;
using server.model;
using System;

namespace client.service.plugins.serverPlugins.clients
{
    public class ClientsMessageHelper
    {
        public ClientsMessageHelper()
        {
        }

        public SimpleSubPushHandler<ClientsModel> OnServerClientsData { get; } = new SimpleSubPushHandler<ClientsModel>();

    }
}
