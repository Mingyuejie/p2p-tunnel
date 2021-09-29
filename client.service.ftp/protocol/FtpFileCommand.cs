﻿using ProtoBuf;

namespace client.service.ftp.protocol
{
    [ProtoContract]
    public class FtpFileCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE;
        [ProtoMember(2)]
        public long SessionId { get; set; }
        [ProtoMember(3)]
        public byte[] Data { get; set; }
        [ProtoMember(4)]
        public long Size { get; set; }
        [ProtoMember(5)]
        public long Md5 { get; set; }
        [ProtoMember(6)]
        public string Name { get; set; }
       
    }

    [ProtoContract]
    public class FtpFileEndCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_END;
        [ProtoMember(2)]
        public long SessionId { get; set; }
        [ProtoMember(3)]
        public long Md5 { get; set; }
    }
}
