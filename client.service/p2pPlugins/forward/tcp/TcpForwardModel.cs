﻿using client.service.serverPlugins.clients;
using ProtoBuf;
using System;

namespace client.service.p2pPlugins.forward.tcp
{
    [ProtoContract]
    public class TcpForwardModel
    {
        public TcpForwardModel() { }

        [ProtoMember(1)]
        public long RequestId { get; set; } = 0;

        [ProtoMember(2)]
        public byte[] Buffer { get; set; } = new byte[0];

        [ProtoMember(3, IsRequired = true)]
        public TcpForwardType Type { get; set; } = TcpForwardType.REQUEST;

        [ProtoMember(4)]
        public string TargetIp { get; set; } = string.Empty;

        [ProtoMember(5)]
        public int TargetPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.UNALIVE;
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardType
    {
        REQUEST, RESPONSE, FAIL, RESPONSE_END
    }


    public class TcpForwardRecordBaseModel
    {
        public int ID { get; set; } = 0;
        public string SourceIp { get; set; } = "0.0.0.0";
        public int SourcePort { get; set; } = 8080;
        public string TargetName { get; set; } = string.Empty;
        public string TargetIp { get; set; } = "127.0.0.1";
        public int TargetPort { get; set; } = 8080;
        public bool Listening { get; set; } = false;
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.UNALIVE;
    }

    public class TcpForwardRecordModel : TcpForwardRecordBaseModel
    {
        public ClientInfo Client { get; set; }
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardAliveTypes : int
    {
        //长连接
        ALIVE,
        //短连接
        UNALIVE
    }
}