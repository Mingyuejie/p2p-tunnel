using server;
using server.model;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.plugins.serverPlugins
{
    public interface IServerRequest
    {

        Task<MessageRequestResponeWrap> SendReply<T>(SendEventArg<T> arg);
        Task<MessageRequestResponeWrap> SendReply(SendEventArg<byte[]> arg);
        Task<bool> SendOnly<T>(SendEventArg<T> arg);
        Task<bool> SendOnly(SendEventArg<byte[]> arg);
    }


    public class SendEventArg<T>
    {
        public IConnection Connection { get; set; }
        public T Data { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Timeout { get; set; } = 0;
    }
}
