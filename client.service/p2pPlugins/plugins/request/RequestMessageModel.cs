using ProtoBuf;
using System;

namespace client.service.p2pPlugins.plugins.request
{
    [ProtoContract]
    public class MessageRequestModel : IP2PMessageBase
    {
        public MessageRequestModel() { }

        [ProtoMember(1, IsRequired = true)]
        public P2PDataMessageTypes Type { get; } = P2PDataMessageTypes.REQUEST;

        [ProtoMember(2)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(3)]
        public long RequestId { get; set; } = 0;

        [ProtoMember(4)]
        public string Path { get; set; } = string.Empty;

        [ProtoMember(6, IsRequired = true)]
        public MessageRequestResultCodes Code { get; set; } = MessageRequestResultCodes.OK;

        [ProtoMember(7, IsRequired = true)]
        public RequestTypes RequestType { get; set; } = RequestTypes.REQUEST;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum MessageRequestResultCodes : short
    {
        OK, NOTFOUND, FAIL
    }
}
