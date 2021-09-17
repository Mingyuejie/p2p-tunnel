using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract]
    public class EmptyModel
    {
        public EmptyModel() { }
        [ProtoMember(1)]
        public byte Id { get; } = 0;

    }
}
