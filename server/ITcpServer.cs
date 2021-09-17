using server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public interface ITcpServer
    {
        public void Start(int port, IPAddress ip = null);
        public void BindAccept(int port, IPAddress ip, CancellationTokenSource tokenSource);
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null);
        public void Stop();
        /// <summary>
        /// 发了等回复
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task<ServerResponeMessageWrap> SendReply<T>(RecvQueueModel<T> msg);
        /// <summary>
        /// 只管发
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        public void SendOnly<T>(RecvQueueModel<T> msg);
    }
}
