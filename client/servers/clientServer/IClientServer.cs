using System.Collections.Generic;
using System.Reflection;

namespace client.servers.clientServer
{
    public interface IClientServer
    {
        public void Start();
        public void LoadPlugins(Assembly[] assemblys);
        public IEnumerable<SettingPluginInfo> GetSettingPlugins();
        public IClientServiceSettingPlugin GetSettingPlugin(string className);
        public IEnumerable<string> GetPlugins();
    }

    public class SettingPluginInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
    }
}
