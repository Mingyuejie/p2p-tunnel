using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.protocol
{
    [ProtoContract, MessagePackObject]
    public class FtpCancelCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true),Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_CANCEL;
        [ProtoMember(2), Key(2)]
        public long SessionId { get; set; }

        [ProtoMember(3), Key(3)]
        public long Md5 { get; set; }
    }

    [ProtoContract, MessagePackObject]
    public class FtpCanceledCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_CANCELED;
        [ProtoMember(2), Key(2)]
        public long SessionId { get; set; }

        [ProtoMember(3), Key(3)]
        public long Md5 { get; set; }
    }
}
