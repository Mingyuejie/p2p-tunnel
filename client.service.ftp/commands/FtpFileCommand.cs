using common.extends;
using MessagePack;
using ProtoBuf;
using System;
using System.Text;

namespace client.service.ftp.commands
{
    [ProtoContract, MessagePackObject]
    public class FtpFileCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE;
        [ProtoMember(2), Key(2)]
        public long Size { get; set; }
        [ProtoMember(3), Key(3)]
        public ulong Md5 { get; set; }
        [ProtoMember(4), Key(4)]
        public string Name { get; set; }

        [ProtoIgnore, IgnoreMember]
        public byte[] MetaData { get; set; }

        [ProtoIgnore, IgnoreMember]
        public ReadOnlyMemory<byte> ReadData { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;

            byte[] sizeByte = Size.GetBytes();

            byte[] md5Byte = Md5.GetBytes();

            byte[] name = Name.GetBytes();
            byte[] nameLength = name.Length.GetBytes();

            MetaData = new byte[
                1 +
                sizeByte.Length +
                md5Byte.Length +
                name.Length + nameLength.Length
            ];

            int index = 1;
            MetaData[0] = cmdByte;

            Array.Copy(sizeByte, 0, MetaData, index, sizeByte.Length);
            index += sizeByte.Length;

            Array.Copy(md5Byte, 0, MetaData, index, md5Byte.Length);
            index += md5Byte.Length;

            Array.Copy(nameLength, 0, MetaData, index, nameLength.Length);
            index += 4;
            Array.Copy(name, 0, MetaData, index, name.Length);
            index += name.Length;

            return MetaData;
        }
        public void WriteData(byte[] data, byte[] resultBytes)
        {
            Array.Copy(MetaData, 0, resultBytes, 0, MetaData.Length);
            Array.Copy(data, 0, resultBytes, MetaData.Length, data.Length);
        }
        public void DeBytes(ReadOnlyMemory<byte> memory)
        {
            int index = 1;

            Size = memory.Span.Slice(index, 8).ToInt64();
            index += 8;

            Md5 = memory.Span.Slice(index, 8).ToUInt64();
            index += 8;

            int nameLength = memory.Span.Slice(index, 4).ToInt32();
            index += 4;
            Name = memory.Span.Slice(index, nameLength).GetString();
            index += nameLength;

            ReadData = memory.Slice(index);
        }

    }

    [ProtoContract, MessagePackObject]
    public class FtpFileEndCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; set; } = FtpCommand.FILE_END;
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
    public class FtpFileErrorCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; set; } = FtpCommand.FILE_ERROR;
        [ProtoMember(2), Key(2)]
        public ulong Md5 { get; set; }
        [ProtoMember(3), Key(3)]
        public string Msg { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;
            byte[] md5Byte = Md5.GetBytes();

            byte[] msg = Msg.GetBytes();
            byte[] msgLength = msg.Length.GetBytes();

            var bytes = new byte[
                1 +
                md5Byte.Length + msg.Length + msgLength.Length
            ];
            bytes[0] = cmdByte;
            int index = 1;

            Array.Copy(md5Byte, 0, bytes, index, md5Byte.Length);
            index += md5Byte.Length;

            Array.Copy(msgLength, 0, bytes, index, msgLength.Length);
            index += 4;

            if (msg.Length > 0)
            {
                Array.Copy(msg, 0, bytes, index, msg.Length);
                index += msg.Length;
            }
            return bytes;
        }

        public void DeBytes(ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
            int index = 1;

            Md5 = bytes.Span.Slice(index).ToUInt64();
            index += 8;

            int msgLength = bytes.Span.Slice(index).ToInt32();
            index += 4;
            if (msgLength > 0)
            {
                Msg = bytes.Span.Slice(index, msgLength).GetString();
            }
        }
    }
}
