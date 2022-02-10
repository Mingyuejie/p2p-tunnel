using MessagePack;
using System;

namespace client.service.ftp.commands
{
    [MessagePackObject]
    public class FtpCurrentPathCommand : IFtpCommandBase
    {
        [Key(1)]
        public FtpCommand Cmd { get; set; } = FtpCommand.CURRENT_PATH;

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;

            var bytes = new byte[1] { cmdByte };
            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
        }
    }
}
