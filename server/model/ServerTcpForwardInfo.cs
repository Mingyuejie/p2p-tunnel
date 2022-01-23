using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace server.model
{
    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardRegisterParamsInfo
    {
        [ProtoMember(1), Key(1)]
        public ServerTcpForwardWebInfo[] Web { get; set; } = Array.Empty<ServerTcpForwardWebInfo>();
        [ProtoMember(2), Key(2)]
        public ServerTcpForwardTunnelInfo[] Tunnel { get; set; } = Array.Empty<ServerTcpForwardTunnelInfo>();
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum ServerTcpForwardRegisterResponseCodes : byte
    {
        [Description("成功")]
        OK,
        [Description("已禁用")]
        DISABLED,
        [Description("出错了")]
        ERROR
    }

    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardWebInfo
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 0;
        [ProtoMember(2), Key(2)]
        public Dictionary<string, ServerTcpForwardWebItemInfo> Forwards { get; set; } = new Dictionary<string, ServerTcpForwardWebItemInfo>();
    }
    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardWebItemInfo
    {
        [ProtoMember(2), Key(2)]
        public int TargetPort { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }
    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardTunnelInfo
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 0;
        [ProtoMember(2), Key(2)]
        public int TargetPort { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }

    [ProtoContract, MessagePackObject]
    public class ServerTcpForwardInfo
    {
        public ServerTcpForwardInfo() { }

        [ProtoMember(1), Key(1)]
        public ulong RequestId { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public byte[] Buffer { get; set; } = new byte[0];

        [ProtoMember(3, IsRequired = true), Key(3)]
        public ServerTcpForwardTypes Type { get; set; } = ServerTcpForwardTypes.REQUEST;

        [ProtoMember(4), Key(4)]
        public string TargetIp { get; set; } = string.Empty;

        [ProtoMember(5), Key(5)]
        public int TargetPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true), Key(6)]
        public ServerTcpForwardAliveTypes AliveType { get; set; } = ServerTcpForwardAliveTypes.WEB;

        [ProtoMember(7), Key(7)]
        public byte Compress { get; set; } = 0;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum ServerTcpForwardAliveTypes : byte
    {
        [Description("长链接")]
        TUNNEL,
        [Description("短链接")]
        WEB
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum ServerTcpForwardTypes : byte
    {
        [Description("请求")]
        REQUEST,
        [Description("回复")]
        RESPONSE,
        [Description("失败")]
        FAIL,
        [Description("关闭")]
        CLOSE
    }


}
