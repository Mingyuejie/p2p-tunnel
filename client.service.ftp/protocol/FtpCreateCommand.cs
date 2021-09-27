using ProtoBuf;

namespace client.service.ftp.protocol
{
    [ProtoContract]
    public class FtpCreateCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.CREATE;

        [ProtoMember(2)]
        public string Path { get; set; }
    }
}
