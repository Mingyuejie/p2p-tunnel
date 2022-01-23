using client.messengers.register;
using client.servers.clientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.messengers.register
{
    public class RegisterClientPushMsg : IClientPushMsg
    {
        private readonly RegisterStateInfo registerState;
        private readonly Config config;
        public RegisterClientPushMsg(RegisterStateInfo registerState, Config config)
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

}
