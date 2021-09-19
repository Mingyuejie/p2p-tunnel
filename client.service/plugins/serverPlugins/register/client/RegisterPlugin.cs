using client.plugins.serverPlugins.register;
using client.servers.clientServer;
using client.service.servers.clientServer;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.register.client
{
    public class RegisterPlugin : IClientServicePlugin
    {
        private readonly RegisterHelper registerHelper;
        private readonly RegisterState registerState;
        private readonly RegisterEventHandles registerEventHandles;
        private readonly Config config;
        public RegisterPlugin(RegisterHelper registerHelper, RegisterState registerState, RegisterEventHandles registerEventHandles, Config config)
        {
            this.registerHelper = registerHelper;
            this.registerState = registerState;
            this.registerEventHandles = registerEventHandles;
            this.config = config;
        }

        public async Task Start(ClientServicePluginExcuteWrap arg)
        {
            var result = await registerHelper.Start();
            if (!result.Data)
            {
                arg.SetCode(-1, result.ErrorMsg);
            }
        }

        public async Task Stop(ClientServicePluginExcuteWrap arg)
        {
            await registerEventHandles.SendExitMessage();
        }

        public RegisterInfo Info(ClientServicePluginExcuteWrap arg)
        {
            return new RegisterInfo
            {
                ClientConfig = config.Client,
                ServerConfig = config.Server,
                LocalInfo = registerState.LocalInfo,
                RemoteInfo = registerState.RemoteInfo,
            };
        }
    }

    public class RegisterPushMsgPlugin : IClientServerPushMsgPlugin
    {
        private readonly RegisterState registerState;
        private readonly Config config;
        public RegisterPushMsgPlugin(RegisterState registerState, Config config)
        {
            this.registerState = registerState;
            this.config = config;
        }

        public object Info()
        {
            return new RegisterInfo
            {
                ClientConfig = config.Client,
                ServerConfig = config.Server,
                LocalInfo = registerState.LocalInfo,
                RemoteInfo = registerState.RemoteInfo,
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
