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
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; set; } = FtpCommand.DOWNLOAD;

        [ProtoMember(2), Key(2)]
        public string Path { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;

            byte[] path = Encoding.UTF8.GetBytes(Path);
            byte[] pathLength = BitConverter.GetBytes(path.Length);

            var bytes = new byte[
                1 +
                path.Length + pathLength.Length
            ];
            bytes[0] = cmdByte;

            int index = 1;

            Array.Copy(pathLength, 0, bytes, index, pathLength.Length);
            index += 4;

            if(path.Length > 0)
            {
                Array.Copy(path, 0, bytes, index, path.Length);
                index += path.Length;
            }
            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
            int index = 1;

            int pathLength = BitConverter.ToInt32(bytes.Span.Slice(index));
            index += 4;

            if(pathLength > 0)
            {
                Path = Encoding.UTF8.GetString(bytes.Span.Slice(index, pathLength));
            }
        }
    }
}
