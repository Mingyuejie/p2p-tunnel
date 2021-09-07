using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract]
    public class P2PModel : IModelBase
    {
        public P2PModel() { }

        [ProtoMember(1, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.P2P;

        [ProtoMember(2)]
        public byte[] Data { get; set; } = System.Array.Empty<byte>();

        [ProtoMember(3)]
        public long FormId { get; set; } = 0;

        [ProtoMember(4)]
        public byte DataType { get; set; } = 0;
    }
}
