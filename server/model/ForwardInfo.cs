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
    public class ForwardParamsInfo
    {
        public ForwardParamsInfo() { }

        [ProtoMember(1), Key(1)]
        public ulong ToId { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
