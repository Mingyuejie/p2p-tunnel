using client.service.messengers.register;
using server;
using System.Threading.Tasks;

namespace client.service.messengers.reset
{
    public class ResetMessenger : IMessenger
    {
        private readonly RegisterHelper registerHelper;
        public ResetMessenger(RegisterHelper registerHelper)
        {
            this.registerHelper = registerHelper;
        }

        public async Task Execute(IConnection connection)
        {
            await registerHelper.Register();
        }
    }
}
