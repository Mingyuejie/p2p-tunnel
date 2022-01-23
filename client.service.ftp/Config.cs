using client.servers.clientServer;
using System.IO;
using System.Threading.Tasks;

namespace client.service.ftp
{
    public class Config : ClientConfigureInfoBase
    {
        private string serverRoot = string.Empty;
        public string ServerRoot
        {
            get
            {
                return serverRoot;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    serverRoot = Directory.GetCurrentDirectory();
                }
                else
                {
                    serverRoot = new DirectoryInfo(value).FullName;
                }
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public string ServerCurrentPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string ClientRootPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string ClientCurrentPath { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool Enable { get; set; } = false;
        public int UploadNum { get; set; } = 10;
        public int ReadWriteBufferSize { get; set; } = 10 * 1024 * 1024;
        public int SendPacketSize { get; set; } = 32 * 1024;


        public static async Task<Config> ReadConfig()
        {
            return await FromFile<Config>("ftp-appsettings.json") ?? new Config();
        }

        public async Task SaveConfig()
        {
            Config config = await ReadConfig();

            config.ServerRoot = ServerRoot;
            config.Password = Password;
            config.Enable = Enable;
            config.UploadNum = UploadNum;
            config.ReadWriteBufferSize = ReadWriteBufferSize;
            config.SendPacketSize = SendPacketSize;

            await ToFile(config, "ftp-appsettings.json");
        }
    }
}
