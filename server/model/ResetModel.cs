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
    [ProtoContract]
    public class ResetModel : IModelBase
    {
        public ResetModel() { }

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
        public MessageTypes MsgType { get; } = MessageTypes.SERVER_RESET;
    }
}
