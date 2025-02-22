﻿using common.extends;
using MessagePack;
using System;
using System.Text;

namespace client.service.ftp.commands
{
    [MessagePackObject]
    public class FtpDelCommand : IFtpCommandBase
    {
        [Key(1)]
        public FtpCommand Cmd { get; set; } = FtpCommand.DELETE;

        [Key(2)]
        public string Path { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;

            byte[] path = Path.GetBytes();
            byte[] pathLength = path.Length.GetBytes();

            var bytes = new byte[
                1 +
                path.Length + pathLength.Length
            ];
            bytes[0] = cmdByte;

            int index = 1;

            Array.Copy(pathLength, 0, bytes, index, pathLength.Length);
            index += 4;

            if (path.Length > 0)
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

            int pathLength = bytes.Span.Slice(index).ToInt32();
            index += 4;

            if (pathLength > 0)
            {
                Path = bytes.Span.Slice(index, pathLength).GetString();
            }
        }
    }
}
