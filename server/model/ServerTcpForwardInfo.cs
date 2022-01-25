using MessagePack;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace server.model
{
    [MessagePackObject]
    public class ServerTcpForwardRegisterParamsInfo
    {
        [Key(1)]
        public ServerTcpForwardWebInfo[] Web { get; set; } = Array.Empty<ServerTcpForwardWebInfo>();
        [Key(2)]
        public ServerTcpForwardTunnelInfo[] Tunnel { get; set; } = Array.Empty<ServerTcpForwardTunnelInfo>();
    }

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

    [MessagePackObject]
    public class ServerTcpForwardWebInfo
    {
        [Key(1)]
        public int Port { get; set; } = 0;
        [Key(2)]
        public Dictionary<string, ServerTcpForwardWebItemInfo> Forwards { get; set; } = new Dictionary<string, ServerTcpForwardWebItemInfo>();
    }
    [MessagePackObject]
    public class ServerTcpForwardWebItemInfo
    {
        [Key(2)]
        public int TargetPort { get; set; } = 0;
        [Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }
    [MessagePackObject]
    public class ServerTcpForwardTunnelInfo
    {
        [Key(1)]
        public int Port { get; set; } = 0;
        [Key(2)]
        public int TargetPort { get; set; } = 0;
        [Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class ServerTcpForwardInfo
    {
        public ServerTcpForwardInfo() { }

        [Key(1)]
        public ulong RequestId { get; set; } = 0;

        [Key(2)]
        public byte[] Buffer { get; set; } = new byte[0];

        [Key(3)]
        public ServerTcpForwardTypes Type { get; set; } = ServerTcpForwardTypes.REQUEST;

        [Key(4)]
        public string TargetIp { get; set; } = string.Empty;

        [Key(5)]
        public int TargetPort { get; set; } = 0;

        [Key(6)]
        public ServerTcpForwardAliveTypes AliveType { get; set; } = ServerTcpForwardAliveTypes.WEB;

        [Key(7)]
        public byte Compress { get; set; } = 0;
    }

    [Flags]
    public enum ServerTcpForwardAliveTypes : byte
    {
        [Description("长链接")]
        TUNNEL,
        [Description("短链接")]
        WEB
    }

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
