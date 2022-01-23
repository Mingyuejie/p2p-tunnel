using System.Collections.Generic;
using System.Reflection;

namespace client.servers.clientServer
{
    public interface IClientServer
    {
        public void Start();
        public void LoadPlugins(Assembly[] assemblys);
        public IEnumerable<ClientServiceConfigureInfo> GetConfigures();
        public IClientConfigure GetConfigure(string className);
        public IEnumerable<string> GetServices();
    }

    public class ClientServiceConfigureInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public bool Enable { get; set; } = false;
    }
}
