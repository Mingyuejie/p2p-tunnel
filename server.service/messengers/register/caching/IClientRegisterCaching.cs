using common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.service.messengers.register.caching
{
    public interface IClientRegisterCaching
    {
        public SimpleSubPushHandler<string> OnChanged { get; }

        public bool Get(ulong id,out RegisterCacheInfo client);
        public RegisterCacheInfo GetBySameGroup(string groupid, string name);
        public List<RegisterCacheInfo> GetAll();
        public ulong Add(RegisterCacheInfo model);
        public Task<bool> Remove(ulong id);
        public Task<bool> Notify(IConnection connection);
    }
}
