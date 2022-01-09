using client.plugins.serverPlugins.register;
using client.servers.clientServer;
using client.service.servers.clientServer;
using MessagePack;
using ProtoBuf;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.register.client
{
    public class RegisterPlugin : IClientServicePlugin
    {
        private readonly RegisterHelper registerHelper;
        private readonly RegisterState registerState;
        private readonly RegisterMessageHelper registerMessageHelper;
        private readonly Config config;
        public RegisterPlugin(RegisterHelper registerHelper, RegisterState registerState, RegisterMessageHelper registerMessageHelper, Config config)
        {
            this.registerHelper = registerHelper;
            this.registerState = registerState;
            this.registerMessageHelper = registerMessageHelper;
            this.config = config;
        }

        public async Task<bool> Start(ClientServicePluginExecuteWrap arg)
        {
            var result = await registerHelper.Register();
            if (!result.Data)
            {
                arg.SetCode(-1, result.ErrorMsg);
            }
            return result.Data;
        }

        public async Task Stop(ClientServicePluginExecuteWrap arg)
        {
            await registerHelper.Exit();
        }

        public RegisterInfo Info(ClientServicePluginExecuteWrap arg)
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

        public RegisterInfo Info()
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

    [ProtoContract, MessagePackObject]
    public class RegisterInfo
    {
        [ProtoMember(1), Key(1)]
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        [ProtoMember(2), Key(2)]
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
        [ProtoMember(3), Key(3)]
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
        [ProtoMember(4), Key(4)]
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
    }
}
