using client.messengers.clients;
using client.servers.clientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.messengers.clients
{
    public class ClientsClientPushMsg : IClientPushMsg
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public ClientsClientPushMsg(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        public IEnumerable<ClientInfo> List()
        {
            return clientInfoCaching.All();
        }
    }
}
