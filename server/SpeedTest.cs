using common.extends;
using MessagePack;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace server
{
    internal class SpeedTest
    {
        static void Main(string[] args)
        {
            Test1();
            Console.ReadLine();
        }


        static void Test1()
        {
            int[] kbs = new int[] { 1, 1, 5, 10, 20, 32, 64, 128 };
            int times = 100000;

            for (int i = 0; i < kbs.Length; i++)
            {
                var (ms, ticks) = MemoryTest(kbs[i], times);
                Console.WriteLine($"包大小：{kbs[i]}KB,次数：{times}, 平均{(double)ms / times}ms,{ticks / times}ticks");
                Console.WriteLine("================================");
            }
        }

        static (long, long) MemoryTest(int kb, int times)
        {
            byte[] bytes = new byte[kb * 1024];
            ReceiveBufferTest cacheBuffer = new ReceiveBufferTest();

            FtpFileCommandTest cmd = new FtpFileCommandTest
            {
                Md5 = 1,
                Size = 1,
                Name = "xxxxxxx"
            };
            cmd.ToBytes();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < times; i++)
            {
                var cmdWriteData = cmd.WriteData(bytes);
                MessageRequestWrapTest wrap = new MessageRequestWrapTest
                {
                    RequestId = 1,
                    Content = cmdWriteData,
                    Path = "xxx/xxxx"
                };
                var data = wrap.ToArray();

                cacheBuffer.AddRange(data, data.Length);
                do
                {
                    int packageLen = cacheBuffer.ArrayData.ToInt32(0);
                    if (packageLen > cacheBuffer.Size - 4)
                    {
                        break;
                    }

                    MessageRequestWrapTest wrap1 = new MessageRequestWrapTest();
                    wrap1.FromArray(cacheBuffer.ArrayData, 0, packageLen);

                    FtpFileCommandTest cmd2 = new FtpFileCommandTest();
                    cmd2.FromBytes(wrap1.Memory);

                    cacheBuffer.RemoveRange(0, packageLen + 4);
                } while (cacheBuffer.Size > 4);

            }
            watch.Stop();
            return (watch.ElapsedMilliseconds, watch.ElapsedTicks);
        }

    }

    [MessagePackObject]
    internal class FtpFileCommandTest : IFtpCommandBaseTest
    {
        [Key(1)]
        public FtpCommandTest Cmd { get; } = FtpCommandTest.FILE;
        [Key(2)]
        public long Size { get; set; }
        [Key(3)]
        public ulong Md5 { get; set; }
        [Key(4)]
        public string Name { get; set; }

        [IgnoreMember]
        public byte[] MetaData { get; set; }

        [IgnoreMember]
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

    internal interface IFtpCommandBaseTest
    {
        public FtpCommandTest Cmd { get; }
    }

    [Flags]
    internal enum FtpCommandTest : byte
    {
        [Description("列表")]
        LIST = 0,
        [Description("删除")]
        DELETE = 1,
        [Description("创建")]
        CREATE = 2,
        [Description("下载")]
        DOWNLOAD = 3,
        [Description("传送文件")]
        FILE = 4,
        [Description("传送文件结束")]
        FILE_END = 5,
        [Description("传送文件取消")]
        FILE_CANCEL = 6,
        [Description("传送文件已取消")]
        FILE_CANCELED = 7,
        [Description("传送文件错误")]
        FILE_ERROR = 8,
    }

    internal class MessageRequestWrapTest
    {
        public string Path { get; set; } = string.Empty;
        public ulong RequestId { get; set; } = 0;
        /// <summary>
        /// 发送数据
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();
        /// <summary>
        /// 接收数据
        /// </summary>
        public ReadOnlyMemory<byte> Memory { get; set; } = Array.Empty<byte>();


        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            byte typeByte = (byte)1;
            byte[] requestIdByte = RequestId.GetBytes();

            byte[] pathByte = Path.GetBytes();
            byte[] pathLengthByte = pathByte.Length.GetBytes();

            int packetLength = 1 + requestIdByte.Length + pathByte.Length + pathLengthByte.Length + Content.Length;
            byte[] packetLengthByte = packetLength.GetBytes();
            packetLength += packetLengthByte.Length;

            byte[] res = new byte[packetLength];

            int index = 0;
            Array.Copy(packetLengthByte, 0, res, index, packetLengthByte.Length);
            index += packetLengthByte.Length;

            res[index] = typeByte;
            index += 1;

            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            Array.Copy(pathLengthByte, 0, res, index, pathLengthByte.Length);
            index += pathLengthByte.Length;
            Array.Copy(pathByte, 0, res, index, pathByte.Length);
            index += pathByte.Length;

            Array.Copy(Content, 0, res, index, Content.Length);
            index += Content.Length;

            return res;
        }

        public void FromArray(byte[] bytes, int index, int length)
        {
            index += 1;

            RequestId = bytes.ToUInt64(index);
            index += 8;

            int pathLength = bytes.ToInt32(index);
            index += 4;
            Path = bytes.GetString(index, pathLength);
            index += pathLength;

            Memory = bytes.AsMemory(index, length - index);
        }
    }

    internal class ReceiveBufferTest
    {
        private byte[] items { get; set; } = Array.Empty<byte>();
        private int size = 0;
        public int Size
        {
            get
            {
                return size;
            }
            private set
            {
                if (value == 0)
                {
                    Array.Clear(items, 0, size);
                    items = Array.Empty<byte>();

                }
                else if (value > items.Length)
                {
                    byte[] newItems = new byte[value];
                    Array.Copy(items, newItems, items.Length);
                    items = newItems;
                }
            }
        }

        public byte[] ArrayData
        {
            get
            {
                return items;
            }
        }

        public void AddRange(byte[] data, int length)
        {
            BeResize(length);
            Array.Copy(data, 0, items, size, length);
            size += length;
        }

        public void RemoveRange(int index, int count)
        {
            if (index >= 0 && count > 0 && size - index >= count)
            {
                size -= count;
                if (index < size)
                {
                    Array.Copy(items, index + count, items, index, size - index);
                }
            }
        }

        public void Clear(bool clearData = false)
        {
            size = 0;
            if (clearData)
            {
                Size = 0;
            }
        }

        private void BeResize(int length)
        {
            int _size = size + length;
            if (size + length > items.Length)
            {
                int newsize = items.Length * 2;
                if (newsize < _size)
                {
                    newsize = _size;
                }
                Size = newsize;
            }
        }
    }
}
