using client.plugins.serverPlugins.clients;
using common;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace server.plugins.register.caching
{
    /// <summary>
    /// 客户端缓存
    /// </summary>
    public interface IClientInfoCaching
    {
        public SimpleSubPushHandler<ClientInfo> OnOffline { get; }
        public bool Add(ClientInfo client);
        public bool Get(ulong id, out ClientInfo client);
        public ClientInfo GetByName(string name);
        public IEnumerable<ClientInfo> All();
        public IEnumerable<ulong> AllIds();
        public void Connecting(ulong id,bool val, IConnection connection);
        public void Offline(ulong id);
        public void Offline(ulong id, IConnection connection);
        public void Offline(IConnection connection);
        public void Online(IConnection connection);
        public void Online(ulong id, IConnection connection);
        public void Remove(ulong id);
    }
}
