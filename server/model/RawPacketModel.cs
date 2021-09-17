using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract]
    public class RawPacketModel
    {
        public RawPacketModel() { }

        [ProtoMember(2)]
        public byte[] Data { get; set; } = System.Array.Empty<byte>();

        [ProtoMember(3)]
        public long FormId { get; set; } = 0;

        [ProtoMember(4)]
        public byte LinkLayerType { get; set; } = 0;

        [ProtoMember(5)]
        public long ToId { get; set; } = 0;
    }
}
