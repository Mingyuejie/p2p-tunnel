using ProtoBuf;
using server.model;

namespace server.models
{
    [ProtoContract]
    public class ExitModel : IModelBase
    {
        public ExitModel() { }
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_EXIT;

    }
}
