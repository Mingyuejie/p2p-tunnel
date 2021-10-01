using MessagePack;
using ProtoBuf;

namespace server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class RegisterModel
    {
        public RegisterModel() { }

        /// <summary>
        /// 客户端ID  第一次 UDP注册得到，第二次TCP注册带过来
        /// </summary>
        [ProtoMember(1),Key(1)]
        public long Id { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public string GroupId { get; set; } = string.Empty;
        /// <summary>
        /// 显示名称
        /// </summary>
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
    public class RegisterResultModel
    {
        public RegisterResultModel() { }

        [ProtoMember(1), Key(1)]
        public int Code { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public string Msg { get; set; } = string.Empty;

        [ProtoMember(3), Key(3)]
        public long Id { get; set; } = 0;

        [ProtoMember(4), Key(4)]
        public string Ip { get; set; } = string.Empty;

        [ProtoMember(5), Key(5)]
        public int Port { get; set; } = 0;

        [ProtoMember(6), Key(6)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(7), Key(7)]
        public string GroupId { get; set; } = string.Empty;

        [ProtoMember(8), Key(8)]
        public string Mac { get; set; } = string.Empty;

        [ProtoMember(9), Key(9)]
        public int LocalTcpPort { get; set; } = 0;

        [ProtoMember(10), Key(10)]
        public int LocalUdpPort { get; set; } = 0;
    }
}
