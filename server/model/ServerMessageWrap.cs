using common.extends;
using System;
using System.ComponentModel;
using System.Text;

namespace server.model
{
    public class MessageRequestWrap
    {
        public Memory<byte> Path { get; set; } = Memory<byte>.Empty;
        //public ReadOnlyMemory<byte> PathMemory { get;private set; }

        public ulong RequestId { get; set; } = 0;
        /// <summary>
        /// 发送数据
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();
        private byte[] content = Array.Empty<byte>();

        /// <summary>
        /// 接收数据
        /// </summary>
        public ReadOnlyMemory<byte> Memory { get; private set; } = Array.Empty<byte>();

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(ServerType type)
        {
            byte typeByte = (byte)MessageTypes.REQUEST;
            byte[] requestIdByte = RequestId.GetBytes();
            byte[] pathLengthByte = Path.Length.GetBytes();

            byte[] res = new byte[(type == ServerType.TCP ? 4 : 0)
                + 1
                + requestIdByte.Length
                + pathLengthByte.Length + Path.Length
                + Content.Length];

            int index = 0;

            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (res.Length - 4).GetBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = typeByte;
            index += sizeof(byte);

            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            Array.Copy(pathLengthByte, 0, res, index, pathLengthByte.Length);
            index += pathLengthByte.Length;

            Path.Span.CopyTo(res.AsSpan(index, Path.Length));
            index += Path.Length;

            Array.Copy(Content, 0, res, index, Content.Length);
            index += Content.Length;

            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            int index = 1;

            RequestId = memory.Span.Slice(index).ToUInt64();
            index += sizeof(ulong);

            int pathLength = memory.Span.Slice(index).ToInt32();
            index += sizeof(int);

            Path = memory.Slice(index, pathLength);
            index += pathLength;

            Memory = memory.Slice(index, memory.Length - index);
        }

        public void Reset()
        {
            Path = Memory<byte>.Empty;
            Content = content;
            Memory = content;
        }
    }
    public class MessageResponseWrap
    {
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
        public ulong RequestId { get; set; } = 0;
        /// <summary>
        /// 发送数据
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();
        private byte[] content = Array.Empty<byte>();
        /// <summary>
        /// 接收数据
        /// </summary>
        public ReadOnlyMemory<byte> Memory { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 转包
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(ServerType type)
        {
            byte[] res = new byte[(type == ServerType.TCP ? 4 : 0)
                + 1
                + 1
                + 8
                + Content.Length];

            int index = 0;
            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = (res.Length - sizeof(int)).GetBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = (byte)MessageTypes.RESPONSE;
            index += sizeof(byte);

            res[index] = (byte)Code;
            index += sizeof(byte);

            byte[] requestIdByte = RequestId.GetBytes();
            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            Array.Copy(Content, 0, res, index, Content.Length);
            index += Content.Length;

            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> memory)
        {
            int index = 1;

            Code = (MessageResponeCodes)memory.Span[index];
            index += sizeof(byte);

            RequestId = memory.Span.Slice(index).ToUInt64();
            index += sizeof(ulong);

            Memory = memory.Slice(index, memory.Length - index);
        }

        public void Reset()
        {
            Content = content;
            Memory = content;
        }
    }

    [Flags]
    public enum MessageResponeCodes : byte
    {
        [Description("成功")]
        OK = 0,
        [Description("网络未连接")]
        NOT_CONNECT = 1,
        [Description("网络资源未找到")]
        NOT_FOUND = 2,
        [Description("网络超时")]
        TIMEOUT = 3,
        [Description("程序错误")]
        ERROR = 4,
    }

    [Flags]
    public enum MessageTypes : byte
    {
        [Description("请求")]
        REQUEST = 0,
        [Description("回复")]
        RESPONSE = 1
    }

    public class MessageRequestParamsInfo<T>
    {
        public IConnection Connection { get; set; }

        public string Path
        {
            set
            {
                memoryPath = value.ToLower().GetBytes().AsMemory();
            }
        }

        private Memory<byte> memoryPath = null;
        public Memory<byte> MemoryPath
        {
            get
            {
                return memoryPath;
            }
            set
            {
                memoryPath = value;
            }
        }

        public T Data { get; set; } = default;
        public ulong RequestId { get; set; } = 0;
        public int Timeout { get; set; } = 15000;
    }

    public class MessageResponseParamsInfo
    {
        public IConnection Connection { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public ulong RequestId { get; set; } = 0;
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
    }
}
