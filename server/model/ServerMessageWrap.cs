using common.extends;
using ProtoBuf;
using System;
using System.ComponentModel;
using System.Text;

namespace server.model
{
    public class MessageRequestWrap
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
        public byte[] ToArray(ServerType type)
        {
            byte typeByte = (byte)MessageTypes.REQUEST;
            byte[] requestIdByte = RequestId.GetBytes();

            byte[] pathByte = Path.GetBytes();
            byte[] pathLengthByte = pathByte.Length.GetBytes();

            int packLength = 1 + requestIdByte.Length + pathByte.Length + pathLengthByte.Length + Content.Length;
            int payloadLength = packLength;
            if (type == ServerType.TCP)
            {
                packLength += 4;
            }

            byte[] res = new byte[packLength];
            int index = 0;

            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = payloadLength.GetBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = typeByte;
            index += 1;

            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            Array.Copy(pathLengthByte, 0, res, index, pathLengthByte.Length);
            index += pathLengthByte.Length;
            Array.Copy(pathByte, 0, res, index, pathByte.Length);
            index += pathByte.Length;

            //Console.WriteLine($"{RequestId} 发送index:{index},content:{Content.Length}");

            Array.Copy(Content, 0, res, index, Content.Length);
            index += Content.Length;

            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(byte[] bytes, int index, int length)
        {
            index += 1;

            RequestId = bytes.ToUInt64(index);
            index += 8;

            int pathLength = bytes.ToInt32(index);
            index += 4;

            Path = bytes.GetString(index, pathLength);
            index += pathLength;

            //Console.WriteLine($"{RequestId} AsMemory index:{index},length:{length},count:{length-index},total:{bytes.Length}");

            Memory = bytes.AsMemory(index, length - index);
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
            byte typeByte = (byte)MessageTypes.RESPONSE;
            byte[] requestIdByte = RequestId.GetBytes();
            short code = (short)Code;
            byte[] codeByte = code.GetBytes();

            int packetLength = 1 + requestIdByte.Length + codeByte.Length + Content.Length;
            int payloadLength = packetLength;
            if (type == ServerType.TCP)
            {
                packetLength += 4;
            }

            byte[] res = new byte[packetLength];
            int index = 0;

            if (type == ServerType.TCP)
            {
                byte[] payloadLengthByte = payloadLength.GetBytes();
                Array.Copy(payloadLengthByte, 0, res, index, payloadLengthByte.Length);
                index += payloadLengthByte.Length;
            }

            res[index] = typeByte;
            index += 1;

            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            Array.Copy(codeByte, 0, res, index, codeByte.Length);
            index += codeByte.Length;

            Array.Copy(Content, 0, res, index, Content.Length);
            index += Content.Length;
            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(byte[] bytes, int index, int length)
        {
            index += 1;

            RequestId = bytes.ToUInt64(index);
            index += 8;

            Code = (MessageResponeCodes)bytes.ToInt16(index);
            index += 2;

            Memory = bytes.AsMemory(index, length - index);
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
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

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
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

        public string Path { get; set; } = string.Empty;
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
