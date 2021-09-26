using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.protocol
{
    [ProtoContract]
    public class FtpCreateCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd => FtpCommand.CREATE;

        [ProtoMember(2)]
        public string Path { get; set; }
    }
}
