﻿using ProtoBuf;

namespace server.model
{
    [ProtoContract]
    public class HeartModel
    {
        public HeartModel() { }

        [ProtoMember(1)]
        public long TargetId { get; set; } = 0;

        /// <summary>
        /// 来源id  -1为服务器  其它为客户端
        /// </summary>
        [ProtoMember(2)]
        public long SourceId { get; set; } = 0;
    }
}
