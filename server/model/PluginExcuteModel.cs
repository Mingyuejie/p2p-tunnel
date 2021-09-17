using ProtoBuf;
using server.packet;
using System;
using System.Net;
using System.Net.Sockets;

namespace server.model
{
    [ProtoContract]
    public class ServerMessageWrap
    {
        [ProtoMember(1)]
        public string Path { get; set; } = string.Empty;
        [ProtoMember(2)]
        public long RequestId { get; set; } = 0;
        [ProtoMember(3)]
        public byte[] Content { get; set; } = Array.Empty<byte>();

        [ProtoMember(4, IsRequired = true)]
        public ServerResponeCodes Code { get; set; } = ServerResponeCodes.OK;
    }

    public class ServerResponeMessageWrap
    {
        public ServerResponeCodes Code { get; set; } = ServerResponeCodes.OK;
        public string ErrorMsg { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
    public enum ServerResponeCodes
    {
        OK = 200,
        TIMEOUT = 408,
        BAD_GATEWAY = 502,
    }


    public class PluginExcuteModel
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

        public ServerResponeCodes Code { get; set; } = ServerResponeCodes.OK;
        public string ErrorMessage { get; private set; } = string.Empty;
        public void SetCode(ServerResponeCodes code, string errormsg = "")
        {
            Code = code;
            ErrorMessage = errormsg;
        }
        public void SetErrorMessage(string msg)
        {
            ErrorMessage = msg;
        }
    }
}
