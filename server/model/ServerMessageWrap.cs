using MessagePack;
using ProtoBuf;
using server.packet;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace server.model
{
    [ProtoContract, MessagePackObject]
    public class ServerMessageWrap
    {
        [ProtoMember(1), Key(1)]
        public string Path { get; set; } = string.Empty;
        [ProtoMember(2), Key(2)]
        public long RequestId { get; set; } = 0;

        /// <summary>
        /// 写入数据用这个
        /// </summary>
        [ProtoMember(3), Key(3)]
        public byte[] Content { get; set; } = Array.Empty<byte>();

        [ProtoMember(4, IsRequired = true), Key(4)]
        public ServerMessageTypes Type { get; set; } = ServerMessageTypes.REQUEST;
        [ProtoMember(5, IsRequired = true), Key(5)]
        public ServerMessageResponeCodes Code { get; set; } = ServerMessageResponeCodes.OK;
        [ProtoMember(6), Key(6)]
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 读取数据用这个
        /// </summary>
        [IgnoreMember, ProtoIgnore]
        public Memory<byte> Memory { get; set; } = Array.Empty<byte>();


        public byte[] ToArray()
        {
            byte typeByte = (byte)Type;
            byte[] requestIdByte = BitConverter.GetBytes(RequestId);

            byte[] codeByte = BitConverter.GetBytes((int)Code);

            byte[] msg = Encoding.UTF8.GetBytes(Msg);
            byte[] msgLength = BitConverter.GetBytes(msg.Length);

            byte[] pathByte = Encoding.ASCII.GetBytes(Path);
            byte[] pathLengthByte = BitConverter.GetBytes(pathByte.Length);

            byte[] res = new byte[
                1 + 
                requestIdByte.Length + 
                codeByte.Length +
                msgLength.Length + msg.Length +
                pathByte.Length + pathLengthByte.Length +
                Content.Length
            ];

            int index = 1;
            res[0] = typeByte;

            Array.Copy(requestIdByte, 0, res, index, requestIdByte.Length);
            index += requestIdByte.Length;

            Array.Copy(codeByte, 0, res, index, codeByte.Length);
            index += codeByte.Length;

            Array.Copy(msgLength, 0, res, index, msgLength.Length);
            index += 4;
            Array.Copy(msg, 0, res, index, msg.Length);
            index += msg.Length;

            Array.Copy(pathLengthByte, 0, res, index, pathLengthByte.Length);
            index += pathLengthByte.Length;
            Array.Copy(pathByte, 0, res, index, pathByte.Length);
            index += pathByte.Length;

            Array.Copy(Content, 0, res, index, Content.Length);
            index += Content.Length;
            return res;
        }

        public void FromArray(byte[] bytes)
        {
            var span = bytes.AsSpan();

            Type = (ServerMessageTypes)span[0];
            int index = 1;

            RequestId = BitConverter.ToInt64(span.Slice(index, 8));
            index += 8;

            Code = (ServerMessageResponeCodes)BitConverter.ToInt32(span.Slice(index, 4));
            index += 4;

            int msgLength = BitConverter.ToInt32(span.Slice(index, 4));
            index += 4;
            if (msgLength > 0)
            {
                Msg = Encoding.UTF8.GetString(span.Slice(index, msgLength));
                index += msgLength;
            }

            int pathLength = BitConverter.ToInt32(span.Slice(index, 4));
            index += 4;
            Path = Encoding.ASCII.GetString(span.Slice(index, pathLength));
            index += pathLength;

            Memory = new Memory<byte>(bytes, index, span.Length - index);
        }
    }

    public class ServerMessageResponeWrap
    {
        public ServerMessageResponeCodes Code { get; set; } = ServerMessageResponeCodes.OK;
        public string ErrorMsg { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum ServerMessageResponeCodes : int
    {
        OK = 200,
        ACCESS = 403,
        NOT_FOUND = 404,
        TIMEOUT = 408,
        BAD_GATEWAY = 502,
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum ServerMessageTypes : byte
    {
        REQUEST = 0, RESPONSE = 1
    }

    public class PluginParamWrap
    {
        /// <summary>
        /// 来源地址
        /// </summary>
        public IPEndPoint SourcePoint { get; set; }

        /// <summary>
        /// 发送数据
        /// </summary>
        public IPacket Packet { get; set; }

        /// <summary>
        /// 服务类型
        /// </summary>
        public ServerType ServerType { get; set; }

        /// <summary>
        /// 消息对象
        /// </summary>
        public Socket TcpSocket { get; set; }

        public ServerMessageWrap Wrap { get; set; }

        public ServerMessageResponeCodes Code { get; set; } = ServerMessageResponeCodes.OK;
        public string ErrorMessage { get; private set; } = string.Empty;
        public void SetCode(ServerMessageResponeCodes code, string errormsg = "")
        {
            Code = code;
            ErrorMessage = errormsg;
        }
        public void SetErrorMessage(string msg)
        {
            ErrorMessage = msg;
        }
    }

    public class SendMessageWrap<T>
    {
        /// <summary>
        /// 目标地址
        /// </summary>
        public IPEndPoint Address { get; set; } = null;

        /// <summary>
        /// 目标对象
        /// </summary>
        public Socket TcpCoket { get; set; } = null;

        /// <summary>
        /// 发送数据
        /// </summary>
        public T Data { get; set; } = default;
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 超时时间，毫秒 0无限 -1一直超时 最小500
        /// </summary>
        public int Timeout { get; set; } = 15000;

        public string Path { get; set; } = string.Empty;

        public ServerMessageTypes Type { get; set; } = ServerMessageTypes.REQUEST;

        public long RequestId { get; set; } = 0;

        public ServerMessageResponeCodes Code { get; set; } = ServerMessageResponeCodes.OK;
    }
}
