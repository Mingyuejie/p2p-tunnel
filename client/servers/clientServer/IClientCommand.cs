using System.Reflection;

namespace client.servers.clientServer
{
    public interface IClientCommand
    {
        public string Before(string command);
        public string After(string command);
    }
}
