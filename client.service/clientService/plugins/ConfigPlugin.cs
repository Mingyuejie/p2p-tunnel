using client.service.config;
using common;
using common.extends;
using System;

namespace client.service.clientService.plugins
{

    /// <summary>
    /// 客户端配置
    /// </summary>
    public class ConfigPlugin : IClientServicePlugin
    {
        private readonly Config config;
        public ConfigPlugin(Config config)
        {
            this.config = config;
        }

        public void Update(ClientServicePluginExcuteWrap arg)
        {
            SettingModel model = arg.Content.DeJson<SettingModel>();

            config.Client = model.ClientConfig;
            config.Server = model.ServerConfig;
            config.SaveConfig();
        }
    }

    public class SettingModel
    {
        /// <summary>
        /// 客户端配置
        /// </summary>
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        /// <summary>
        /// 服务器配置
        /// </summary>
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
    }
}
