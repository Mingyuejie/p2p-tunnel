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
    public class FtpDownloadCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true),Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.DOWNLOAD;

        [ProtoMember(2),Key(2)]
        public string Path { get; set; }
    }
}
