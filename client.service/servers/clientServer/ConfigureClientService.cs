using client.servers.clientServer;
using common.extends;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.servers.clientServer
{
    public class ConfigureClientService : IClientService
    {
        private readonly IClientServer clientServer;
        public ConfigureClientService(IClientServer clientServer)
        {
            this.clientServer = clientServer;
        }

        public IEnumerable<ClientServiceConfigureInfo> Configures(ClientServiceParamsInfo arg)
        {
            return clientServer.GetConfigures();
        }
        public async Task<object> Configure(ClientServiceParamsInfo arg)
        {
            SaveParamsInfo model = arg.Content.DeJson<SaveParamsInfo>();
            var plugin = clientServer.GetConfigure(model.ClassName);
            if (plugin != null)
            {
                return await plugin.Load();
            }
            return new { };
        }
        public async Task<bool> Enable(ClientServiceParamsInfo arg)
        {
            EnableParamsInfo model = arg.Content.DeJson<EnableParamsInfo>();
            var plugin = clientServer.GetConfigure(model.ClassName);
            if (plugin != null)
            {
                return await plugin.SwitchEnable(model.Enable);
            }
            return false;
        }
        public async Task Save(ClientServiceParamsInfo arg)
        {
            SaveParamsInfo model = arg.Content.DeJson<SaveParamsInfo>();
            var plugin = clientServer.GetConfigure(model.ClassName);
            if (plugin != null)
            {
                string msg = await plugin.Save(model.Content);
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    arg.SetCode(-1, msg);
                }
            }
        }

        public IEnumerable<string> Services(ClientServiceParamsInfo arg)
        {
            return clientServer.GetServices();
        }
    }

    public class SaveParamsInfo
    {
        public string ClassName { get; set; }
        public string Content { get; set; }
    }

    public class EnableParamsInfo
    {
        public string ClassName { get; set; }
        public bool Enable { get; set; }
    }
}
