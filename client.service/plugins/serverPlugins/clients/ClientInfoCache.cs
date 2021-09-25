using client.plugins.serverPlugins.clients;
using common;
using common.extends;
using server.model;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.clients
{
    public class ClientInfoCache : IClientInfoCaching
    {
        private readonly static ConcurrentDictionary<long, ClientInfo> clients = new ConcurrentDictionary<long, ClientInfo>();

        public void UpdateLastTime(long id)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.UpdateLastTime();
            }
        }

        public void UpdateTcpLastTime(long id)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.UpdateTcpLastTime();
            }
        }

        public bool Add(ClientInfo client)
        {
            return clients.TryAdd(client.Id, client);
        }

        public bool Get(long id, out ClientInfo client)
        {
            return clients.TryGetValue(id, out client);
        }

        public ClientInfo GetByName(string name)
        {
            return clients.Values.FirstOrDefault(c=>c.Name == name);
        }

        public IEnumerable<ClientInfo> All()
        {
            return clients.Values;
        }

        public IEnumerable<long> AllIds()
        {
            return clients.Keys;
        }

        public void Offline(long id)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.Offline();
            }
        }

        public void Online(long id, IPEndPoint address)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.Online(address);
            }
        }

        public void OfflineTcp(long id)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.OfflineTcp();
            }
        }

        public void OnlineTcp(long id, Socket socket)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.OnlineTcp(socket);
            }
        }

        public void OfflineBoth(long id)
        {
            _ = clients.TryGetValue(id, out ClientInfo client);
            if (client != null)
            {
                client.Offline();
                client.OfflineTcp();
            }
        }

        public void Remove(long id)
        {
            clients.TryRemove(id, out _);
        }
    }
}
