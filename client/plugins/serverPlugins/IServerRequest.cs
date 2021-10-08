using server;
using server.model;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.plugins.serverPlugins
{
    public interface IServerRequest
    {

        Task<ServerMessageResponeWrap> SendReply<T>(SendEventArg<T> arg);

        bool SendOnly<T>(SendEventArg<T> arg);

        Task<ServerMessageResponeWrap> SendReplyTcp<T>(SendTcpEventArg<T> arg);

        bool SendOnlyTcp<T>(SendTcpEventArg<T> arg);
        bool SendOnlyTcp(SendTcpEventArg<byte[]> arg);
    }



    public class SendEventArg<T>
    {
        public IPEndPoint Address { get; set; }
        public T Data { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Timeout { get; set; } = 0;
        public long Id { get; set; } = 0;
        public long ToId { get; set; } = 0;
    }
    public class SendTcpEventArg<T>
    {
        public Socket Socket { get; set; }
        public T Data { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Timeout { get; set; } = 0;
        public long Id { get; set; } = 0;
        public long ToId { get; set; } = 0;
    }
}
