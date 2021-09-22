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
        public IEnumerable<SettingPluginInfo> GetSettingPlugins();
        public IClientServiceSettingPlugin GetSettingPlugin(int code);
    }

    public class SettingPluginInfo
    {
        public int Code { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
    }
}
