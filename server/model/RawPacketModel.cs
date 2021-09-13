using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract]
    public class RawPacketModel : IModelBase
    {
        public RawPacketModel() { }

        [ProtoMember(1, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_RAW_PACKET;

        [ProtoMember(2)]
        public byte[] Data { get; set; } = System.Array.Empty<byte>();

        [ProtoMember(3)]
        public long FormId { get; set; } = 0;

        [ProtoMember(4)]
        public byte LinkLayerType { get; set; } = 0;

        [ProtoMember(5)]
        public string TargetIp { get; set; } = string.Empty;
    }
}
