﻿using client.plugins.serverPlugins.register;
using client.servers.clientServer;
using client.service.servers.clientServer;
using ProtoBuf;
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

    [ProtoContract]
    public class RegisterInfo
    {
        [ProtoMember(1)]
        public ClientConfig ClientConfig { get; set; } = new ClientConfig();
        [ProtoMember(2)]
        public ServerConfig ServerConfig { get; set; } = new ServerConfig();
        [ProtoMember(3)]
        public LocalInfo LocalInfo { get; set; } = new LocalInfo();
        [ProtoMember(4)]
        public RemoteInfo RemoteInfo { get; set; } = new RemoteInfo();
    }
}