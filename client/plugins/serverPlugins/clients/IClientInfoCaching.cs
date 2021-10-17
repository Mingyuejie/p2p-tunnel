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
        public SimplePushSubHandler<ClientInfo> OnOffline { get; }
        public SimplePushSubHandler<ClientInfo> OnTcpOffline { get; }

        public void UpdateLastTime(long id);
        public void UpdateTcpLastTime(long id);
        public bool Add(ClientInfo client);
        public bool Get(long id, out ClientInfo client);
        public ClientInfo GetByName(string name);
        public IEnumerable<ClientInfo> All();
        public IEnumerable<long> AllIds();
        public void Offline(long id);
        public void Online(long id, IPEndPoint address);
        public void OfflineTcp(long id);
        public void OnlineTcp(long id, Socket socket);
        public void OfflineBoth(long id);
        public void Remove(long id);
        public void MsgTime(long address,long time);
        public void MsgTcpTime(long address, long time);
    }
}
