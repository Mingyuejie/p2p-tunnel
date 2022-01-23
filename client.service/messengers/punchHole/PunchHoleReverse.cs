using client.messengers.punchHole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.messengers.punchHole
{
    public class PunchHoleReverse : IPunchHole
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerClient;
        public PunchHoleReverse(PunchHoleMessengerSender punchHoleMessengerClient)
        {

            this.punchHoleMessengerClient = punchHoleMessengerClient;
        }

        public PunchHoleTypes Type => PunchHoleTypes.REVERSE;

        public void Execute(OnPunchHoleArg arg)
        {
            punchHoleMessengerClient.OnReverse.Push(arg);
        }
    }
}
