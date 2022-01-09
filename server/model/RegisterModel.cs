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
    public class RegisterModel
    {
        public RegisterModel() { }

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
    public class RegisterNotifyModel
    {
        public RegisterNotifyModel() { }
    }

    [ProtoContract, MessagePackObject]
    public class RegisterResultModel
    {
        public RegisterResultModel() { }

        [ProtoMember(1), Key(1)]
        public RegisterResultCodes Code { get; set; } = RegisterResultCodes.OK;

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
        public enum RegisterResultCodes : byte
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
}
