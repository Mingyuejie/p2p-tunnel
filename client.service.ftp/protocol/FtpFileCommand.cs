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
        public long SessionId { get; set; }
        [ProtoMember(3), Key(3)]
        public byte[] Data { get; set; }
        [ProtoMember(4), Key(4)]
        public long Size { get; set; }
        [ProtoMember(5), Key(5)]
        public long Md5 { get; set; }
        [ProtoMember(6), Key(6)]
        public string Name { get; set; }


        public byte[] ToBytes()
        {
            byte[] sessionIdBytes = BitConverter.GetBytes(SessionId);
            byte[] sizeBytes = BitConverter.GetBytes(Size);
            byte[] md5Bytes = BitConverter.GetBytes(Md5);
            byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
            byte[] nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);

            var res = new byte[1 + sessionIdBytes.Length + sizeBytes.Length + md5Bytes.Length + nameBytes.Length + nameLengthBytes.Length + Data.Length];

            int index = 0;

            Array.Copy(sessionIdBytes, 0, res, index, sessionIdBytes.Length);
            index += sessionIdBytes.Length;

            Array.Copy(sizeBytes, 0, res, index, sizeBytes.Length);
            index += sizeBytes.Length;

            Array.Copy(md5Bytes, 0, res, index, md5Bytes.Length);
            index += md5Bytes.Length;

            Array.Copy(nameLengthBytes, 0, res, index, nameLengthBytes.Length);
            index += nameLengthBytes.Length;

            Array.Copy(nameBytes, 0, res, index, nameBytes.Length);
            index += nameBytes.Length;

            Array.Copy(Data, 0, res, index, Data.Length);
            index += Data.Length;

            return res;
        }

        public void FromBytes(byte[] arr)
        {
            Span<byte> bytes = arr.AsSpan();

            int index = 0;

            SessionId = BitConverter.ToInt64(bytes.Slice(index, 8));
            index += 8;

            Size = BitConverter.ToInt64(bytes.Slice(index, 8));
            index += 8;

            Md5 = BitConverter.ToInt64(bytes.Slice(index, 8));
            index += 8;

            Span<byte> length = bytes.Slice(index, 4);
            int nameLength = BitConverter.ToInt32(length);
            index += 4;

            Name = Encoding.UTF8.GetString(bytes.Slice(index, nameLength));
            index += nameLength;

            Data = bytes.Slice(index, bytes.Length - index).ToArray();
        }

    }

    [ProtoContract, MessagePackObject]
    public class FtpFileEndCommand : IFtpCommandBase
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpCommand Cmd { get; } = FtpCommand.FILE_END;
        [ProtoMember(2), Key(2)]
        public long SessionId { get; set; }
        [ProtoMember(3), Key(3)]
        public long Md5 { get; set; }
    }
}
