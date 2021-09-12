using ProtoBuf;

namespace server.model
{
    /// <summary>
    /// 客户端注册数据
    /// </summary>
    [ProtoContract]
    public class RegisterModel : IModelBase
    {
        public RegisterModel() { }

        /// <summary>
        /// 客户端ID  第一次 UDP注册得到，第二次TCP注册带过来
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        [ProtoMember(2)]
        public string GroupId { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        [ProtoMember(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_REGISTER;

        [ProtoMember(5)]
        public string LocalIps { get; set; } = string.Empty;

        [ProtoMember(6)]
        public string Mac { get; set; } = string.Empty;

        [ProtoMember(7)]
        public int LocalTcpPort { get; set; } = 0;

        [ProtoMember(8)]
        public int LocalUdpPort { get; set; } = 0;
    }

    [ProtoContract]
    public class RegisterResultModel : IModelBase
    {
        public RegisterResultModel() { }

        [ProtoMember(1)]
        public int Code { get; set; } = 0;

        [ProtoMember(2)]
        public string Msg { get; set; } = string.Empty;

        [ProtoMember(3)]
        public long Id { get; set; } = 0;

        [ProtoMember(4, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_REGISTER_RESULT;

        [ProtoMember(5)]
        public string Ip { get; set; } = string.Empty;

        [ProtoMember(6)]
        public int Port { get; set; } = 0;

        [ProtoMember(7)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(8)]
        public string GroupId { get; set; } = string.Empty;

        [ProtoMember(9)]
        public string Mac { get; set; } = string.Empty;

        [ProtoMember(10)]
        public int LocalTcpPort { get; set; } = 0;

        [ProtoMember(11)]
        public int LocalUdpPort { get; set; } = 0;
    }
}
