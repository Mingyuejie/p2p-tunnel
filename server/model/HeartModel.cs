using MessagePack;
using ProtoBuf;

namespace server.model
{
    [ProtoContract, MessagePackObject]
    public class HeartModel
    {
        public HeartModel() { }
    }
}
