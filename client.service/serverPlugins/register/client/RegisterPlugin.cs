﻿using client.service.clientService;
using client.service.config;
using client.service.serverPlugins.register;
using common;
using System.Threading.Tasks;

namespace client.service.serverPlugins.register.client
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

    public class RegisterInfo
    {
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
    }
}
