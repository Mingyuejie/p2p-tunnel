using client.service.config;
using client.service.serverPlugins.register;
using common;
using System.Threading.Tasks;

namespace client.service.clientService.plugins
{
    public class RegisterPlugin : IClientServicePlugin
    {
        public void Start(ClientServicePluginExcuteWrap arg)
        {
            RegisterHelper.Instance.Start((msg) =>
            {
                if (!string.IsNullOrWhiteSpace(msg))
                {

                    arg.SetResultCode(-1, msg);
                }
                arg.Callback(arg, null);
            });

        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            RegisterEventHandles.Instance.SendExitMessage();
            arg.Callback(arg, null);
        }

        public void Info(ClientServicePluginExcuteWrap arg)
        {
            arg.Callback(arg, new RegisterInfo
            {
                ClientConfig = AppShareData.Instance.ClientConfig,
                ServerConfig = AppShareData.Instance.ServerConfig,
                LocalInfo = AppShareData.Instance.LocalInfo,
                RemoteInfo = AppShareData.Instance.RemoteInfo,
            });
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
