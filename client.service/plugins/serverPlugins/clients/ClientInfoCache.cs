using client.plugins.serverPlugins.clients;
using common;
using common.extends;
using server;
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
        private readonly static ConcurrentDictionary<ulong, ClientInfo> clients = new ConcurrentDictionary<ulong, ClientInfo>();

        public SimpleSubPushHandler<ClientInfo> OnOffline { get; } = new SimpleSubPushHandler<ClientInfo>();

        public bool Add(ClientInfo client)
        {
            return clients.TryAdd(client.Id, client);
        }

        public bool Get(ulong id, out ClientInfo client)
        {
            return clients.TryGetValue(id, out client);
        }

        public ClientInfo GetByName(string name)
        {
            return clients.Values.FirstOrDefault(c => c.Name == name);
        }

        public IEnumerable<ClientInfo> All()
        {
            return clients.Values;
        }

        public IEnumerable<ulong> AllIds()
        {
            return clients.Keys;
        }

        public void Connecting(ulong id, bool val, IConnection connection)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                if(connection.ServerType == ServerType.UDP)
                {
                    client.Connecting = val;
                }
                else
                {
                    client.TcpConnecting = val;
                }
            }
        }

        public void Offline(ulong id)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.Offline();
                client.OfflineTcp();
                OnOffline.Push(client);
            }
        }

        public void Offline(ulong id, IConnection connection)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.Offline(connection);
                OnOffline.Push(client);
            }
        }
        public void Offline(IConnection connection)
        {
            if (clients.TryGetValue(connection.ConnectId, out ClientInfo client))
            {
                client.Online(connection);
            }
        }

        public void Remove(ulong id)
        {
            clients.TryRemove(id, out _);
        }

        public void Online(IConnection connection)
        {
            if (clients.TryGetValue(connection.ConnectId, out ClientInfo client))
            {
                client.Online(connection);
            }
        }
        public void Online(ulong id, IConnection connection)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                connection.ConnectId = id;
                client.Online(connection);
            }
        }

    }
}
