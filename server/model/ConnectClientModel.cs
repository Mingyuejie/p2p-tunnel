using ProtoBuf;

namespace server.model
{
    /// <summary>
    /// 告诉服务器，我要连接哪个客户端
    /// </summary>
    [ProtoContract]
    public class ConnectClientModel : IModelBase
    {
        public ConnectClientModel() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 目标客户端id
        /// </summary>
        [ProtoMember(2)]
        public long ToId { get; set; } = 0;

        /// <summary>
        /// 消息类型
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_P2P;
    }

    /// <summary>
    /// 反向链接  A让B链接自己
    /// </summary>
    [ProtoContract]
    public class ConnectClientReverseModel : IModelBase
    {
        public ConnectClientReverseModel() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 目标客户端id
        /// </summary>
        [ProtoMember(2)]
        public long ToId { get; set; } = 0;

        /// <summary>
        /// 消息类型
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_P2P_REVERSE;
    }

    /// <summary>
    /// 第一步
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep1Model : IModelBase
    {
        public ConnectClientStep1Model() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 来源客户端名字
        /// </summary>
        [ProtoMember(2)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 来源客户端ip
        /// </summary>
        [ProtoMember(3)]
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// 来源客户端端口
        /// </summary>
        [ProtoMember(4)]
        public int Port { get; set; } = 0;

        [ProtoMember(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.P2P_STEP_1;

        [ProtoMember(7)]
        public string LocalIps { get; set; } = string.Empty;

        [ProtoMember(8)]
        public int LocalTcpPort { get; set; } = 0;

    }

    [ProtoContract]
    public class ConnectClientStep1AckModel : IModelBase
    {
        public ConnectClientStep1AckModel() { }

        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        [ProtoMember(2, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.P2P_STEP_1_ACK;
    }

    /// <summary>
    /// 第一步结果
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep1ResultModel : IModelBase
    {
        public ConnectClientStep1ResultModel() { }

        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 对方id
        /// </summary>
        [ProtoMember(2)]
        public long ToId { get; set; } = 0;

        /// <summary>
        /// 消息类型
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_P2P_STEP_1_RESULT;


    }

    /// <summary>
    /// 第二步
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep2Model : IModelBase
    {
        public ConnectClientStep2Model() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 来源客户端名字
        /// </summary>
        [ProtoMember(2)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 来源客户端ip
        /// </summary>
        [ProtoMember(3)]
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// 来源客户端端口
        /// </summary>
        [ProtoMember(4)]
        public int Port { get; set; } = 0;

        [ProtoMember(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.P2P_STEP_2;

        [ProtoMember(7)]
        public string LocalIps { get; set; } = string.Empty;

        [ProtoMember(8)]
        public int LocalTcpPort { get; set; } = 0;

    }

    /// <summary>
    /// 
    /// 第二步重试
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep2RetryModel : IModelBase
    {
        public ConnectClientStep2RetryModel() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        [ProtoMember(2)]
        public long ToId { get; set; } = 0;

        /// <summary>
        /// 来源客户端ip
        /// </summary>
        [ProtoMember(3)]
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// 来源客户端端口
        /// </summary>
        [ProtoMember(4)]
        public int Port { get; set; } = 0;

        [ProtoMember(5)]
        public int TcpPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_P2P_STEP_2_RETRY;

        [ProtoMember(7)]
        public string LocalIps { get; set; } = string.Empty;

        [ProtoMember(8)]
        public int LocalTcpPort { get; set; } = 0;

    }
    /// <summary>
    /// 第二步失败
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep2FailModel : IModelBase
    {
        public ConnectClientStep2FailModel() { }

        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 对方id
        /// </summary>
        [ProtoMember(2)]
        public long ToId { get; set; } = 0;

        /// <summary>
        /// 消息类型
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_P2P_STEP_2_FAIL;
    }

    /// <summary>
    /// 第二步停止
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep2StopModel : IModelBase
    {
        public ConnectClientStep2StopModel() { }

        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 对方id
        /// </summary>
        [ProtoMember(2)]
        public long ToId { get; set; } = 0;

        /// <summary>
        /// 消息类型
        /// </summary>
        [ProtoMember(3, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_P2P_STEP_2_STOP;
    }


    /// <summary>
    /// 第三步
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep3Model : IModelBase
    {
        [ProtoMember(1, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.P2P_STEP_3;

        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(2)]
        public long Id { get; set; } = 0;
    }

    /// <summary>
    /// 第四步
    /// </summary>
    [ProtoContract]
    public class ConnectClientStep4Model : IModelBase
    {
        [ProtoMember(1, IsRequired = true)]
        public MessageTypes MsgType { get; } = MessageTypes.P2P_STEP_4;

        /// <summary>
        /// 我的id
        /// </summary>
        [ProtoMember(2)]
        public long Id { get; set; } = 0;
    }
}
