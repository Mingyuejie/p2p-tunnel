using MessagePack;
using ProtoBuf;
using server.packet;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        public byte[] ToArray()
        {
            byte typeByte = (byte)MessageType.REQUEST;
            byte[] requestIdByte = BitConverter.GetBytes(RequestId);

            byte[] pathByte = Encoding.ASCII.GetBytes(Path);
            byte[] pathLengthByte = BitConverter.GetBytes(pathByte.Length);

            byte[] res = new byte[
                1 +
                requestIdByte.Length +
                pathByte.Length + pathLengthByte.Length +
                Content.Length
            ];

            int index = 1;
            res[0] = typeByte;

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
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="bytes"></param>
        public void FromArray(Memory<byte> bytes)
        {
            Span<byte> span = bytes.Span;
            int index = 1;

            RequestId = BitConverter.ToUInt64(span.Slice(index, 8));
            index += 8;

            int pathLength = BitConverter.ToInt32(span.Slice(index, 4));
            index += 4;
            Path = Encoding.ASCII.GetString(span.Slice(index, pathLength));
            index += pathLength;

            Memory = bytes.Slice(index, span.Length - index);
        }
    }

    public class MessageResponseWrap
    {
        public MessageResponeCode Code { get; set; } = MessageResponeCode.OK;
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
            byte typeByte = (byte)MessageType.RESPONSE;
            byte[] requestIdByte = BitConverter.GetBytes(RequestId);
            short code = (short)Code;
            byte[] codeByte = BitConverter.GetBytes(code);

            byte[] res = new byte[
                1 +
                requestIdByte.Length +
                codeByte.Length +
                Content.Length
            ];

            int index = 1;
            res[0] = typeByte;

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
        public void FromArray(Memory<byte> bytes)
        {
            Span<byte> span = bytes.Span;
            int index = 1;

            RequestId = BitConverter.ToUInt64(span.Slice(index, 8));
            index += 8;

            Code = (MessageResponeCode)BitConverter.ToInt16(span.Slice(index, 2));
            index += 2;

            Memory = bytes.Slice(index, span.Length - index);
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum MessageResponeCode : byte
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
    public enum MessageType : byte
    {
        [Description("请求")]
        REQUEST = 0,
        [Description("回复")]
        RESPONSE = 1
    }

    public class PluginParamWrap
    {
        public IConnection Connection { get; set; }

        public MessageRequestWrap Wrap { get; set; }

        public MessageResponeCode Code { get; set; } = MessageResponeCode.OK;
    }

    public class MessageRequestParamsWrap<T>
    {
        public IConnection Connection { get; set; }

        public string Path { get; set; } = string.Empty;
        public T Data { get; set; } = default;

        public ulong RequestId { get; set; } = 0;
        public int Timeout { get; set; } = 15000;
    }
    public class MessageResponseParamsWrap
    {
        public IConnection Connection { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public ulong RequestId { get; set; } = 0;
        public MessageResponeCode Code { get; set; } = MessageResponeCode.OK;
    }
}
