using MessagePack;
using System;
using System.ComponentModel;

namespace server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    [MessagePackObject]
    public class RegisterParamsInfo
    {
        public RegisterParamsInfo() { }

        [Key(1)]
        public ulong Id { get; set; } = 0;

        [Key(2)]
        public string GroupId { get; set; } = string.Empty;

        [Key(3)]
        public string Name { get; set; } = string.Empty;

        [Key(4)]
        public string LocalIps { get; set; } = string.Empty;

        [Key(5)]
        public string Mac { get; set; } = string.Empty;

        [Key(6)]
        public int LocalTcpPort { get; set; } = 0;

        [Key(7)]
        public int LocalUdpPort { get; set; } = 0;
    }

    [MessagePackObject]
    public class RegisterResultInfo
    {
        public RegisterResultInfo() { }

        [Key(1)]
        public RegisterResultInfoCodes Code { get; set; } = RegisterResultInfoCodes.OK;

        [Key(2)]
        public ulong Id { get; set; } = 0;

        [Key(3)]
        public string Ip { get; set; } = string.Empty;

        [Key(4)]
        public int Port { get; set; } = 0;

        [Key(5)]
        public int TcpPort { get; set; } = 0;

        [Key(6)]
        public string GroupId { get; set; } = string.Empty;


        [Flags]
        public enum RegisterResultInfoCodes : byte
        {
            [Description("成功")]
            OK = 0,
            [Description("存在同名")]
            SAME_NAMES = 1,
            [Description("验证未通过")]
            VERIFY = 1,
            [Description("出错")]
            UNKNOW = 255
        }

    }

    [MessagePackObject]
    public class TunnelRegisterParamsInfo
    {
        public TunnelRegisterParamsInfo() { }

        [Key(1)]
        public string TunnelName { get; set; } = string.Empty;

        [Key(2)]
        public int LocalPort { get; set; } = 0;

        [Key(3)]
        public int Port { get; set; } = 0;
    }

    [MessagePackObject]
    public class TunnelRegisterInfo
    {
        public TunnelRegisterInfo() { }

        [Key(1)]
        public TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes Code { get; set; } = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.OK;
        [Key(2)]
        public int Port { get; set; } = 0;
    }

    [MessagePackObject]
    public class TunnelRegisterResultInfo
    {
        public TunnelRegisterResultInfo() { }

        [Key(1)]
        public TunnelRegisterResultInfoCodes Code { get; set; } = TunnelRegisterResultInfoCodes.OK;

        [Flags]
        public enum TunnelRegisterResultInfoCodes : byte
        {
            [Description("成功")]
            OK = 0,
            [Description("存在同名")]
            SAME_NAMES = 1,
            [Description("验证未通过")]
            VERIFY = 2,
            [Description("未连接服务器")]
            UN_CONNECT = 3,
            [Description("出错")]
            UNKNOW = 255
        }
    }
}
