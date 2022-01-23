using server.model;
using System.Threading.Tasks;

namespace client.messengers.register
{
    public interface IDataTunnelRegister
    {
        Task<TunnelRegisterResultInfo> Register(string tunnelName);
    }
}
