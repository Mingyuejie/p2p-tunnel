using server;
using server.model;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.plugins.serverPlugins
{
    public interface IServerRequest
    {

        Task<MessageRequestResponeWrap> SendReply<T>(SendArg<T> arg);
        Task<MessageRequestResponeWrap> SendReply(SendArg<byte[]> arg);
        Task<bool> SendOnly<T>(SendArg<T> arg);
        Task<bool> SendOnly(SendArg<byte[]> arg);
    }


    public class SendArg<T>
    {
        public IConnection Connection { get; set; }
        public T Data { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Timeout { get; set; } = 0;
    }
}
