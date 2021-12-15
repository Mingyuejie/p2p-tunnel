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

        public SimplePushSubHandler<ClientInfo> OnOffline { get; } = new SimplePushSubHandler<ClientInfo>();
        public SimplePushSubHandler<ClientInfo> OnTcpOffline { get; } = new SimplePushSubHandler<ClientInfo>();

        public void UpdateLastTime(long id)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.UpdateLastTime();
            }
        }

        public void UpdateTcpLastTime(long id)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
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
            return clients.Values.FirstOrDefault(c => c.Name == name);
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
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                msgTimeUdp.TryRemove(client.UdpAddressId, out _);
                client.Offline();
                OnOffline.Push(client);
            }
        }
        public void Online(long id, IPEndPoint address)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.Online(address);
            }
        }

        public void OfflineTcp(long id)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                msgTimeTcp.TryRemove(client.TcpAddressId, out _);
                client.OfflineTcp();
                OnTcpOffline.Push(client);
            }
        }
        public void OnlineTcp(long id, Socket socket)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                client.OnlineTcp(socket);
            }
        }

        public void OfflineBoth(long id)
        {
            if (clients.TryGetValue(id, out ClientInfo client))
            {
                msgTimeUdp.TryRemove(client.UdpAddressId, out _);
                msgTimeTcp.TryRemove(client.TcpAddressId, out _);
                client.Offline();
                client.OfflineTcp();
                OnTcpOffline.Push(client);
            }
        }

        public void Remove(long id)
        {
            clients.TryRemove(id, out _);
        }


        private readonly static ConcurrentDictionary<long, ClientInfo> msgTimeUdp = new ConcurrentDictionary<long, ClientInfo>();
        private readonly static ConcurrentDictionary<long, ClientInfo> msgTimeTcp = new ConcurrentDictionary<long, ClientInfo>();

        public void MsgTime(long address, long time)
        {
            if (msgTimeUdp.TryGetValue(address, out ClientInfo client))
            {
                client.LastTime = time;
            }
            else
            {
                client = All().FirstOrDefault(c => c.UdpAddressId == address);
                if (client != null)
                {
                    client.LastTime = time;
                    msgTimeUdp.TryAdd(address, client);
                }
            }
        }

        public void MsgTcpTime(long address, long time)
        {
            if (msgTimeTcp.TryGetValue(address, out ClientInfo client))
            {
                client.TcpLastTime = time;
            }
            else
            {
                client = All().FirstOrDefault(c => c.TcpAddressId == address);
                if (client != null)
                {
                    client.TcpLastTime = time;
                    msgTimeTcp.TryAdd(address, client);
                }
            }
        }
    }
}
