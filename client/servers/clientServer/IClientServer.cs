using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace client.servers.clientServer
{
    public interface IClientServer
    {
        public void Start();
        public void LoadPlugins(Assembly[] assemblys);
    }
}
