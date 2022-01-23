using common.extends;
using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.commands
{
    [ProtoContract, MessagePackObject]
    public class FtpCancelCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; set; } = FtpCommand.FILE_CANCEL;

        [ProtoMember(2), Key(2)]
        public ulong Md5 { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;
            byte[] md5Byte = Md5.GetBytes();

            var bytes = new byte[
                1 +
                md5Byte.Length
            ];
            bytes[0] = cmdByte;

            Array.Copy(md5Byte, 0, bytes, 1, md5Byte.Length);

            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
            Md5 = bytes.Span.Slice(1).ToUInt64();
        }
    }

    [ProtoContract, MessagePackObject]
    public class FtpCanceledCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; set; } = FtpCommand.FILE_CANCELED;

        [ProtoMember(2), Key(2)]
        public ulong Md5 { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;
            byte[] md5Byte = Md5.GetBytes();

            var bytes = new byte[
                1 +
                md5Byte.Length
            ];
            bytes[0] = cmdByte;

            Array.Copy(md5Byte, 0, bytes, 1, md5Byte.Length);

            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
            Md5 = bytes.Span.Slice(1).ToUInt64();
        }
    }
}
