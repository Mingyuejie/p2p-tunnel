using MessagePack;
using System;

namespace server.model
{
    /// <summary>
    /// 转发
    /// </summary>
    [ MessagePackObject]
    public class ForwardParamsInfo
    {
        public ForwardParamsInfo() { }

        [Key(1)]
        public ulong ToId { get; set; } = 0;

        [Key(2)]
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
