using client.servers.clientServer;
using common.extends;
using System.Threading.Tasks;

namespace client.service.servers.clientServer
{
    public class ConfigClientService : IClientService
    {
        private readonly Config config;
        public ConfigClientService(Config config)
        {
            this.config = config;
        }

        public async Task Update(ClientServiceParamsInfo arg)
        {
            ConfigureParamsInfo model = arg.Content.DeJson<ConfigureParamsInfo>();

            config.Client = model.ClientConfig;
            config.Server = model.ServerConfig;
            await config.SaveConfig();
        }
    }

    public class ConfigureParamsInfo
    {
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
    }
}
