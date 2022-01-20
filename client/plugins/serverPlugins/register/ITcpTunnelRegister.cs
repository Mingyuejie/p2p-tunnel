using server.model;
using System.Threading.Tasks;

namespace client.plugins.serverPlugins.register
{
    public interface ITunnelRegister
    {
        Task<TunnelRegisterResult> Register(TunnelRegisterParam param);
    }

}
