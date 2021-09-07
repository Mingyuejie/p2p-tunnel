using System.Net;
using System.Net.Sockets;

namespace server.model
{
    /// <summary>
    /// 消息发送队列对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RecvQueueModel<T>
    {
        /// <summary>
        /// 目标地址
        /// </summary>
        public IPEndPoint Address { get; set; } = null;

        /// <summary>
        /// 目标对象
        /// </summary>
        public Socket TcpCoket { get; set; } = null;

        /// <summary>
        /// 发送数据
        /// </summary>
        public T Data { get; set; } = default;

        /// <summary>
        /// 超时时间，毫秒 0无限 -1一直超时 最小500
        /// </summary>
        public int Timeout { get; set; } = 0;
    }
}
