using client.messengers.clients;
using MessagePack;
using System;
using System.ComponentModel;

namespace client.service.tcpforward
{
    [MessagePackObject]
    public class TcpForwardInfo
    {
        public TcpForwardInfo() { }

        [Key(1)]
        public ulong RequestId { get; set; } = 0;

        [Key(2)]
        public byte[] Buffer { get; set; } = Array.Empty<byte>();

        [Key(3)]
        public TcpForwardTypes Type { get; set; } = TcpForwardTypes.REQUEST;

        [Key(4)]
        public string TargetIp { get; set; } = string.Empty;

        [Key(5)]
        public int TargetPort { get; set; } = 0;

        [Key(6)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;

        [Key(7)]
        public byte Compress { get; set; } = 0;

        [Key(8)]
        public ulong FromID { get; set; } = 0;
    }


    [Flags]
    public enum TcpForwardTypes : byte
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

    [ MessagePackObject]
    public class TcpForwardRecordInfoBase
    {
        [ Key(1)]
        public int ID { get; set; } = 0;
        [ Key(2)]
        public string SourceIp { get; set; } = "0.0.0.0";
        [Key(3)]
        public int SourcePort { get; set; } = 8080;
        [ Key(4)]
        public string TargetName { get; set; } = string.Empty;
        [ Key(5)]
        public string TargetIp { get; set; } = "127.0.0.1";
        [ Key(6)]
        public int TargetPort { get; set; } = 8080;
        [ Key(7)]
        public bool Listening { get; set; } = false;
        [ Key(8)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;

        [Key(9)]
        public string Desc { get; set; } = "";

        [Key(10)]
        public bool Editable { get; set; } = true;

        [Key(11)]
        public string Group { get; set; } = "";
    }

    public class TcpForwardRecordInfo : TcpForwardRecordInfoBase
    {
        public ClientInfo Client { get; set; }
    }

    [Flags]
    public enum TcpForwardAliveTypes : byte
    {
        [Description("长连接")]
        TUNNEL,
        [Description("短连接")]
        WEB
    }
}
