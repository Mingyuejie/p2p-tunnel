using client.servers.clientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ddns
{
    public class Config : ClientConfigureInfoBase
    {
        public bool Enable { get; set; } = false;
        public int Interval { get; set; } = 100;
        public PlatformInfo[] Platforms { get; set; } = Array.Empty<PlatformInfo>();

        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("ddns-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();
            config.Platforms = Platforms;
            config.Enable = Enable;
            config.Interval = Interval;

            await ToFile(config, "ddns-appsettings.json");
        }
    }

    public class PlatformInfo
    {
        public string Name { get; set; } = string.Empty;
        public GroupInfo[] Groups { get; set; } = Array.Empty<GroupInfo>();

    }
    public class GroupInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public List<string> Records { get; set; } = new List<string>();
    }
}
