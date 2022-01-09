using MessagePack;
using ProtoBuf;

namespace client.service.ftp.protocol
{
    [ProtoContract, MessagePackObject]
    public class FtpCreateCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true),Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.CREATE;

        [ProtoMember(2), Key(2)]
        public string Path { get; set; }
    }
}
