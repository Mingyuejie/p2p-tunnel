using MessagePack;
using ProtoBuf;
using System;
using System.Text;

namespace client.service.ftp.protocol
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

            byte[] sizeByte = BitConverter.GetBytes(Size);

            byte[] md5Byte = BitConverter.GetBytes(Md5);

            byte[] name = Encoding.UTF8.GetBytes(Name);
            byte[] nameLength = BitConverter.GetBytes(name.Length);

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
        public byte[] WriteData(byte[] data)
        {
            var res = new byte[MetaData.Length + data.Length];
            Array.Copy(MetaData, 0, res, 0, MetaData.Length);
            Array.Copy(data, 0, res, MetaData.Length, data.Length);
            return res;
        }
        public void FromBytes(ReadOnlyMemory<byte> memory)
        {
            int index = 1;

            Size = BitConverter.ToInt64(memory.Span.Slice(index, 8));
            index += 8;

            Md5 = BitConverter.ToUInt64(memory.Span.Slice(index, 8));
            index += 8;

            int nameLength = BitConverter.ToInt32(memory.Span.Slice(index, 4));
            index += 4;
            Name = Encoding.UTF8.GetString(memory.Span.Slice(index, nameLength));
            index += nameLength;

            ReadData = memory.Slice(index);
        }

    }

    [ProtoContract, MessagePackObject]
    public class FtpFileEndCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_END;
        [ProtoMember(2), Key(2)]
        public ulong Md5 { get; set; }
    }

    [ProtoContract, MessagePackObject]
    public class FtpFileErrorCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_ERROR;
        [ProtoMember(2), Key(2)]
        public ulong Md5 { get; set; }
        [ProtoMember(3), Key(3)]
        public string Msg { get; set; }
    }
}
