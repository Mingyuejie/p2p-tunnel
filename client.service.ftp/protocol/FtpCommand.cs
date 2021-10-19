using MessagePack;
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
        FILE_CANCEL = 6,
        FILE_CANCELED = 7,
    }

    [ProtoContract]
    public interface IFtpCommandBase
    {
        public FtpCommand Cmd { get; }
        public long SessionId { get; set; }
    }

    [ProtoContract, MessagePackObject]
    public class FtpCommandBase : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; set; }

        [ProtoMember(2), Key(2)]
        public long SessionId { get; set; }

        [ProtoMember(3), Key(3)]
        public Memory<byte> Data { get; set; }
    }

}
