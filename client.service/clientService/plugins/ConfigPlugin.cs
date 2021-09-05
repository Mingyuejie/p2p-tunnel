using client.service.config;
using common;
using System;

namespace client.service.clientService.plugins
{

    /// <summary>
    /// 客户端配置
    /// </summary>
    public class ConfigPlugin : IClientServicePlugin
    {
        public void Update(ClientServicePluginExcuteWrap arg)
        {
            SettingModel model = Helper.DeJsonSerializer<SettingModel>(arg.Content);

            AppShareData.Instance.ClientConfig = model.ClientConfig;
            AppShareData.Instance.ServerConfig = model.ServerConfig;
            AppShareData.Instance.SaveConfig();
            arg.Callback(arg, null);
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
