using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.protocol
{
    [ProtoContract]
    public class FtpFileCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd => FtpCommand.FILE;

        [ProtoMember(2)]
        public byte[] Data { get; set; }

        [ProtoMember(3)]
        public long Size { get; set; }

        [ProtoMember(4)]
        public string Md5 { get; set; }

        [ProtoMember(5)]
        public string Name { get; set; }
    }

    [ProtoContract]
    public class FtpFileEndCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd => FtpCommand.FILE_END;
        [ProtoMember(4)]
        public string Md5 { get; set; }
    }
}
