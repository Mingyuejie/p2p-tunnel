using ProtoBuf;
using System;

namespace client.service.ftp.protocol
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FtpCommand : byte
    {
        LIST = 0,
        DELETE = 1,
        CREATE = 2,
        DOWNLOAD = 3,
        FILE = 4,
        FILE_END = 5
    }

    [ProtoContract]
    public interface IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; }
    }
}
