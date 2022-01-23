using server.model;

namespace client.messengers.punchHole
{
    public interface IPunchHoleStepInfo
    {
        PunchHoleTypes PunchType { get; }
        public PunchForwardTypes PunchForwardType { get; }
        public byte PunchStep { get; }
    }
}
