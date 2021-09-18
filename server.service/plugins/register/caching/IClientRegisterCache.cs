using server.model;
using System.Collections.Generic;
using System.Net.Sockets;

namespace server.service.plugins.register.caching
{
    public interface IClientRegisterCaching
    {
        public RegisterCacheModel Get(long id);
        public RegisterCacheModel GetBySameGroup(string groupid, string name);
        public List<RegisterCacheModel> GetAll();
        public long Add(RegisterCacheModel model, long id = 0);
        public bool UpdateTcpInfo(long id, Socket socket, int port, string groupId);
        public void Remove(long id);
        public void UpdateTime(long id);
        public bool Verify(long id, PluginParamWrap data);
    }
}
