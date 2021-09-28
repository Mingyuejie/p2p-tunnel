using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.protocol
{
    [ProtoContract]
    public class FtpDelCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; } = FtpCommand.DELETE;
        [ProtoMember(2)]
        public long SessionId { get; set; }

        [ProtoMember(3)]
        public string Path { get; set; }
    }
}
