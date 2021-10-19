using MessagePack;
using ProtoBuf;
using System;
using System.Text;

namespace client.service.ftp.protocol
{
    [ProtoContract, MessagePackObject]
    public class FtpFileCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE;
        [ProtoMember(2), Key(2)]
        public long SessionId { get; set; }
        [ProtoMember(3), Key(3)]
        public byte[] Data { get; set; }
        [ProtoMember(4), Key(4)]
        public long Size { get; set; }
        [ProtoMember(5), Key(5)]
        public long Md5 { get; set; }
        [ProtoMember(6), Key(6)]
        public string Name { get; set; }

    }

    [ProtoContract, MessagePackObject]
    public class FtpFileEndCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_END;
        [ProtoMember(2), Key(2)]
        public long SessionId { get; set; }
        [ProtoMember(3), Key(3)]
        public long Md5 { get; set; }
    }
}
