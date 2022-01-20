using common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.service.plugins.register.caching
{
    public interface IClientRegisterCaching
    {
        public SimpleSubPushHandler<string> OnChanged { get; }

        public bool Get(ulong id,out RegisterCacheModel client);
        public RegisterCacheModel GetBySameGroup(string groupid, string name);
        public List<RegisterCacheModel> GetAll();
        public ulong Add(RegisterCacheModel model);
        public Task<bool> Remove(ulong id);
        public Task<bool> Notify(IConnection connection);
    }
}
