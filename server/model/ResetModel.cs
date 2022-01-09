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
    /// 重启
    /// </summary>
    [ProtoContract, MessagePackObject]
    public class ResetModel
    {
        public ResetModel() { }

        /// <summary>
        /// 目标客户端id
        /// </summary>
        [ProtoMember(1),Key(1)]
        public ulong ToId { get; set; } = 0;
    }
}
