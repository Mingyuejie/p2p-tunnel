using System.Net;

namespace server
{
    public interface IUdpServer : IServer
    {
        public IConnection CreateConnection(IPEndPoint address);
    }
}
