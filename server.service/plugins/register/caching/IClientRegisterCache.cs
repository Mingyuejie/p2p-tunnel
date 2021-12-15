using common;
using server.model;
using System.Collections.Generic;
using System.Net.Sockets;

namespace server.service.plugins.register.caching
{
    public interface IClientRegisterCaching
    {
        public SimplePushSubHandler<string> OnChanged { get; }

        public RegisterCacheModel Get(long id);
        public RegisterCacheModel GetBySameGroup(string groupid, string name);
        public List<RegisterCacheModel> GetAll();
        public long Add(RegisterCacheModel model);
        public bool UpdateTcpInfo(RegisterCacheUpdateModel model);
        public void Remove(long id);
        public void UpdateTime(long id);
        public bool Verify(long id, PluginParamWrap data);
    }
}
