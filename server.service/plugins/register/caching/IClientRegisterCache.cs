using common;
using server.model;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

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
        public Task<bool> Remove(ulong id);
        public Task<bool> Notify(IConnection connection);
    }
}
