using client.servers.clientServer;
using common.extends;
using System.Threading.Tasks;

namespace client.service.ddns.client
{
    public class DdnsClientConfigure : IClientConfigure
    {
        private readonly Config config;
        public DdnsClientConfigure(Config config)
        {
            this.config = config;
        }

        public string Name => "动态域名解析";

        public string Author => "snltty";

        public string Desc => "ip变化后自动解析域名";

        public bool Enable => config.Enable;

        public async Task<object> Load()
        {
            return await Task.FromResult(config);
        }

        public async Task<string> Save(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();
            config.Enable = _config.Enable;
            config.Interval = _config.Interval;
            config.Platforms = _config.Platforms;
            await config.SaveConfig();
            return string.Empty;
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            return await Task.FromResult(true);
        }
    }
}
