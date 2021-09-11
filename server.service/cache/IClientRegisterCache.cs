using server.model;
using server.service.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace server.service.cache
{
    public interface IClientRegisterCache
    {
        public RegisterCacheModel Get(long id);
        public RegisterCacheModel GetBySameGroup(string groupid, string name);
        public List<RegisterCacheModel> GetAll();
        public long Add(RegisterCacheModel model, long id = 0);
        public bool UpdateTcpInfo(long id, Socket socket, int port, string groupId);
        public void Remove(long id);
        public void UpdateTime(long id);
        public bool Verify(long id, PluginExcuteModel data);
    }
}
