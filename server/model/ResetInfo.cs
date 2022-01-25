using MessagePack;

namespace server.model
{
    /// <summary>
    /// 重启
    /// </summary>
    [ MessagePackObject]
    public class ResetParamsInfo
    {
        public ResetParamsInfo() { }

        /// <summary>
        /// 目标客户端id
        /// </summary>
        [Key(1)]
        public ulong ToId { get; set; } = 0;
    }
}
