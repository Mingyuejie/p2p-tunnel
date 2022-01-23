using MessagePack;
using ProtoBuf;
using System;
using System.ComponentModel;

namespace server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class RegisterParamsInfo
    {
        public RegisterParamsInfo() { }

        [ProtoMember(1), Key(1)]
        public ulong Id { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public string GroupId { get; set; } = string.Empty;

        [ProtoMember(3), Key(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4), Key(4)]
        public string LocalIps { get; set; } = string.Empty;

        [ProtoMember(5), Key(5)]
        public string Mac { get; set; } = string.Empty;

        [ProtoMember(6), Key(6)]
        public int LocalTcpPort { get; set; } = 0;

        [ProtoMember(7), Key(7)]
        public int LocalUdpPort { get; set; } = 0;
    }

    [ProtoContract, MessagePackObject]
    public class RegisterResultInfo
    {
        public RegisterResultInfo() { }

        [ProtoMember(1), Key(1)]
        public RegisterResultInfoCodes Code { get; set; } = RegisterResultInfoCodes.OK;

        [ProtoMember(2), Key(2)]
        public ulong Id { get; set; } = 0;

        [ProtoMember(3), Key(3)]
        public string Ip { get; set; } = string.Empty;

        [ProtoMember(4), Key(4)]
        public int Port { get; set; } = 0;

        [ProtoMember(5), Key(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(6), Key(6)]
        public string GroupId { get; set; } = string.Empty;


        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
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

    [ProtoContract, MessagePackObject]
    public class TunnelRegisterParamsInfo
    {
        public TunnelRegisterParamsInfo() { }

        [ProtoMember(1), Key(1)]
        public string TunnelName { get; set; } = string.Empty;

        [ProtoMember(2), Key(2)]
        public int LocalPort { get; set; } = 0;

        [ProtoMember(3), Key(3)]
        public int Port { get; set; } = 0;
    }

    [ProtoContract, MessagePackObject]
    public class TunnelRegisterInfo
    {
        public TunnelRegisterInfo() { }

        [ProtoMember(1), Key(1)]
        public TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes Code { get; set; } = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.OK;
        [ProtoMember(2), Key(2)]
        public int Port { get; set; } = 0;
    }

    [ProtoContract, MessagePackObject]
    public class TunnelRegisterResultInfo
    {
        public TunnelRegisterResultInfo() { }

        [ProtoMember(1), Key(1)]
        public TunnelRegisterResultInfoCodes Code { get; set; } = TunnelRegisterResultInfoCodes.OK;

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
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
