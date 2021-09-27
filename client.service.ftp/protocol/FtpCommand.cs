using ProtoBuf;
using System;

namespace client.service.ftp.protocol
{
    public enum FtpPluginMode : byte
    {
        SERVER = 0,
        CLIENT = 1,
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FtpCommand : byte
    {
        LIST = 0,
        DELETE = 1,
        CREATE = 2,
        DOWNLOAD = 3,
        FILE = 4,
        FILE_END = 5,
        FILE_PROGRESS = 6
    }

    [ProtoContract]
    public interface IFtpCommandBase
    {
        public FtpCommand Cmd { get; }
    }

    [ProtoContract]
    public class FtpCommandBase : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true)]
        public FtpCommand Cmd { get; }
    }

}
