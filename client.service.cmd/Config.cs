using client.servers.clientServer;
using System.Threading.Tasks;

namespace client.service.cmd
{
    public class Config : ClientConfigureInfoBase
    {
        public string Password { get; set; } = string.Empty;
        public bool Enable { get; set; } = false;

        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("cmd-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();
            config.Password = Password;
            config.Enable = Enable;

            await ToFile(config, "cmd-appsettings.json");
        }
    }
}
