using ProtoBuf;

namespace client.service.ftp.protocol
{
    [ProtoContract]
    public class FtpFileCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE;

        [ProtoMember(2)]
        public byte[] Data { get; set; }

        [ProtoMember(3)]
        public long Size { get; set; }

        [ProtoMember(4)]
        public long Md5 { get; set; }

        [ProtoMember(5)]
        public string Name { get; set; }
    }

    [ProtoContract]
    public class FtpFileEndCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_END;
        [ProtoMember(4)]
        public long Md5 { get; set; }
    }

    [ProtoContract]
    public class FtpFileProgressCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_PROGRESS;
        [ProtoMember(2)]
        public long Md5 { get; set; }
        [ProtoMember(3)]
        public long Index { get; set; }
    }
}
