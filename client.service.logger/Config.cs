using client.servers.clientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.logger
{
    public class Config : ClientConfigureInfoBase
    {
        public bool Enable { get; set; } = false;
        public int MaxLength { get; set; } = 100;

        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("logger-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();
            config.Enable = Enable;

            await ToFile(config, "logger-appsettings.json");
        }
    }
}
