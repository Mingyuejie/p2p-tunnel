using common;
using server.model;
using System.Collections.Generic;
using System.Net.Sockets;

namespace server.service.plugins.register.caching
{
    public interface IClientRegisterCaching
    {
        public SimplePushSubHandler<string> OnChanged { get; }

        public bool Get(ulong id,out RegisterCacheModel client);
        public RegisterCacheModel GetBySameGroup(string groupid, string name);
        public List<RegisterCacheModel> GetAll();
        public ulong Add(RegisterCacheModel model);
        public bool UpdateTcpInfo(RegisterCacheUpdateModel model);
        public void Remove(ulong id);
        public void UpdateTime(ulong id);
        public bool Notify(IConnection connection);
    }
}
