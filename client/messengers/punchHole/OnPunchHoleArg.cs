using server;
using server.model;

namespace client.messengers.punchHole
{
    public class OnPunchHoleArg
    {
        public PunchHoleParamsInfo Data { get; set; }
        public IConnection Connection { get; set; }
    }
}
