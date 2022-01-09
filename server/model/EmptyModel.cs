using MessagePack;
using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract, MessagePackObject]
    public class EmptyModel
    {
        public EmptyModel() { }

    }
}
