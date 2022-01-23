using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.service.messengers.punchHole.tcp.nutssb;
using client.service.messengers.punchHole.udp;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.model;
using System;
using System.Reflection;

namespace client.service.messengers.punchHole
{
    public class PunchHoleMessenger : IMessenger
    {
        private readonly PunchHoleMessengerSender  punchHoleMessengerClient;
        public PunchHoleMessenger(PunchHoleMessengerSender punchHoleMessengerClient)
        {

            this.punchHoleMessengerClient = punchHoleMessengerClient;
        }

        public void Execute(IConnection connection)
        {
            PunchHoleParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<PunchHoleParamsInfo>();

            punchHoleMessengerClient.OnPunchHole(new OnPunchHoleArg
            {
                Data = model,
                Connection = connection
            });
        }
    }

    
}
