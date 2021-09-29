using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.protocol
{
    [ProtoContract]
    public class FtpCancelCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_CANCEL;
        [ProtoMember(2)]
        public long SessionId { get; set; }

        [ProtoMember(3)]
        public long Md5 { get; set; }
    }

    [ProtoContract]
    public class FtpCanceledCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_CANCELED;
        [ProtoMember(2)]
        public long SessionId { get; set; }

        [ProtoMember(3)]
        public long Md5 { get; set; }
    }
}
