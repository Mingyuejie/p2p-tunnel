using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.model
{
    /// <summary>
    /// 转发
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class ForwardModel
    {
        public ForwardModel() { }

        /// <summary>
        /// 来源客户端id
        /// </summary>
        [ProtoMember(1), Key(1)]
        public long Id { get; set; } = 0;

        /// <summary>
        /// 目标客户端id
        /// </summary>
        [ProtoMember(2), Key(2)]
        public long ToId { get; set; } = 0;

        [ProtoMember(3), Key(3)]
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
