using client.service.config;
using client.service.serverPlugins.register;
using common;
using System.Threading.Tasks;

namespace client.service.clientService.plugins
{
    public class RegisterPlugin : IClientServicePlugin
    {
        public async Task Start(ClientServicePluginExcuteWrap arg)
        {
            var result = await RegisterHelper.Instance.Start();
            if (!string.IsNullOrWhiteSpace(result.ErrorMsg))
            {
                arg.SetCode(-1, result.ErrorMsg);
            }
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            RegisterEventHandles.Instance.SendExitMessage();
        }

        public RegisterInfo Info(ClientServicePluginExcuteWrap arg)
        {
            return new RegisterInfo
            {
                ClientConfig = AppShareData.Instance.ClientConfig,
                ServerConfig = AppShareData.Instance.ServerConfig,
                LocalInfo = AppShareData.Instance.LocalInfo,
                RemoteInfo = AppShareData.Instance.RemoteInfo,
            };
        }
    }

    public class RegisterInfo
    {
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
    }
}
