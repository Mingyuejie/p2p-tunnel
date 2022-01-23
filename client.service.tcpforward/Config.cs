using client.servers.clientServer;
using System;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class Config : ClientConfigureInfoBase
    {
        public int[] PortWhiteList { get; set; } = Array.Empty<int>();
        public int[] PortBlackList { get; set; } = Array.Empty<int>();

        public bool Enable { get; set; } = true;
        public int ReceiveBufferSize { get; set; } = 1024;
        public int NumConnections { get; set; } = 3000;

        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("tcpforward-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();

            config.PortBlackList = PortBlackList;
            config.PortWhiteList = PortWhiteList;
            config.Enable = Enable;
            config.ReceiveBufferSize = ReceiveBufferSize;
            config.NumConnections = NumConnections;

            await ToFile(config, "tcpforward-appsettings.json");
        }
    }
}
