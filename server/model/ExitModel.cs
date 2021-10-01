using MessagePack;
using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract, MessagePackObject]
    public class ExitModel
    {
        public ExitModel() { }
        [ProtoMember(1),Key(1)]
        public long Id { get; set; } = 0;

    }
}
