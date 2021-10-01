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
        [ProtoMember(1),Key(1)]
        public string Path { get; set; } = string.Empty;
        [ProtoMember(2), Key(2)]
        public long RequestId { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public byte[] Content { get; set; } = Array.Empty<byte>();
        [ProtoMember(4, IsRequired = true), Key(4)]
        public ServerMessageTypes Type { get; set; } = ServerMessageTypes.REQUEST;
        [ProtoMember(5, IsRequired = true), Key(5)]
        public ServerMessageResponeCodes Code { get; set; } = ServerMessageResponeCodes.OK;

        public byte[] ToArray()
        {

            byte[] byteRequestId = BitConverter.GetBytes(RequestId);
            byte[] bytePath = Encoding.UTF8.GetBytes(Path);
            byte byteType = (byte)Type;
            byte[] byteCode = BitConverter.GetBytes((int)Code);

            byte[] result = new byte[byteRequestId.Length + bytePath.Length + 1 + byteCode.Length + Content.Length + (5 * 4)];

            int distIndex = 0;

            byte[] byteLength = BitConverter.GetBytes(byteRequestId.Length);
            Array.Copy(byteLength, 0, result, distIndex, byteLength.Length);
            distIndex += 4;
            Array.Copy(byteRequestId, 0, result, distIndex, byteRequestId.Length);
            distIndex += byteRequestId.Length;

            byteLength = BitConverter.GetBytes(bytePath.Length);
            Array.Copy(byteLength, 0, result, distIndex, byteLength.Length);
            distIndex += 4;
            Array.Copy(bytePath, 0, result, distIndex, bytePath.Length);
            distIndex += bytePath.Length;

            byteLength = BitConverter.GetBytes(1);
            Array.Copy(byteLength, 0, result, distIndex, byteLength.Length);
            distIndex += 4;
            result[distIndex] = byteType;
            distIndex += 1;

            byteLength = BitConverter.GetBytes(byteCode.Length);
            Array.Copy(byteLength, 0, result, distIndex, byteLength.Length);
            distIndex += 4;
            Array.Copy(byteCode, 0, result, distIndex, byteCode.Length);
            distIndex += byteCode.Length;

            byteLength = BitConverter.GetBytes(Content.Length);
            Array.Copy(byteLength, 0, result, distIndex, byteLength.Length);
            distIndex += 4;
            Array.Copy(Content, 0, result, distIndex, Content.Length);
            distIndex += Content.Length;

            return result;
        }

        public static ServerMessageWrap FromArray(byte[] bytes)
        {
            int distIndex = 0;

            int requestIdLength = BitConverter.ToInt32(bytes.Take(4).ToArray());
            distIndex += 4;
            long requestId = BitConverter.ToInt64(bytes.Skip(distIndex).Take(requestIdLength).ToArray());
            distIndex += requestIdLength;

            int pathLength = BitConverter.ToInt32(bytes.Skip(distIndex).Take(4).ToArray());
            distIndex += 4;
            string path = Encoding.ASCII.GetString(bytes.Skip(distIndex).Take(pathLength).ToArray());
            distIndex += pathLength;


            int typeLength = BitConverter.ToInt32(bytes.Skip(distIndex).Take(4).ToArray());
            distIndex += 4;
            ServerMessageTypes type = (ServerMessageTypes)bytes.Skip(distIndex).Take(typeLength).ToArray()[0];
            distIndex += typeLength;

            int codeLength = BitConverter.ToInt32(bytes.Skip(distIndex).Take(4).ToArray());
            distIndex += 4;
            ServerMessageResponeCodes code = (ServerMessageResponeCodes)BitConverter.ToInt32(bytes.Skip(distIndex).Take(codeLength).ToArray());
            distIndex += codeLength;

            int contentLength = BitConverter.ToInt32(bytes.Skip(distIndex).Take(4).ToArray());
            distIndex += 4;
            byte[] content = bytes.Skip(distIndex).Take(contentLength).ToArray();

            return new ServerMessageWrap
            {
                Code = code,
                Content = content,
                Path = path,
                RequestId = requestId,
                Type = type
            };
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

        /// <summary>
        /// 超时时间，毫秒 0无限 -1一直超时 最小500
        /// </summary>
        public int Timeout { get; set; } = 0;

        public string Path { get; set; } = string.Empty;

        public ServerMessageTypes Type { get; set; } = ServerMessageTypes.REQUEST;

        public long RequestId { get; set; } = 0;

        public ServerMessageResponeCodes Code { get; set; } = ServerMessageResponeCodes.OK;
    }
}
