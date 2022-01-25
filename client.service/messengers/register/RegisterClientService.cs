using client.messengers.register;
using client.servers.clientServer;
using MessagePack;
using System.Threading.Tasks;

namespace client.service.messengers.register
{
    public class RegisterClientService : IClientService
    {
        private readonly RegisterHelper registerHelper;
        private readonly RegisterStateInfo registerState;
        private readonly RegisterMessengerSender registerMessengerClient;
        private readonly Config config;
        public RegisterClientService(RegisterHelper registerHelper, RegisterStateInfo registerState, RegisterMessengerSender registerMessengerClient, Config config)
        {
            this.registerHelper = registerHelper;
            this.registerState = registerState;
            this.registerMessengerClient = registerMessengerClient;
            this.config = config;
        }

        public async Task<bool> Start(ClientServiceParamsInfo arg)
        {
            var result = await registerHelper.Register();
            if (!result.Data)
            {
                arg.SetCode(-1, result.ErrorMsg);
            }
            return result.Data;
        }

        public async Task Stop(ClientServiceParamsInfo arg)
        {
            await registerHelper.Exit();
        }

        public RegisterInfo Info(ClientServiceParamsInfo arg)
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

    [MessagePackObject]
    public class RegisterInfo
    {
        [Key(1)]
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        [Key(2)]
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
        [Key(3)]
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
        [Key(4)]
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
    }
}
