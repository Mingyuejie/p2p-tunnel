using MessagePack;
using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract, MessagePackObject]
    public class RawPacketModel
    {
        public RawPacketModel() { }

        [ProtoMember(1),Key(1)]
        public byte[] Data { get; set; } = System.Array.Empty<byte>();

        [ProtoMember(2), Key(2)]
        public long FormId { get; set; } = 0;

        [ProtoMember(3), Key(3)]
        public byte LinkLayerType { get; set; } = 0;

        [ProtoMember(4), Key(4)]
        public long ToId { get; set; } = 0;
    }
}
