using common;
using server;
using server.model;
using System;

namespace client.service.messengers.clients
{
    public class ClientsMessengerSender
    {
        public ClientsMessengerSender()
        {
        }

        public SimpleSubPushHandler<ClientsInfo> OnServerClientsData { get; } = new SimpleSubPushHandler<ClientsInfo>();

    }
}
