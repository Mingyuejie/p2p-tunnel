using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using client.service.ftp.server.plugin;
using common.extends;
using server.plugins.register.caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.client
{
    public class FtpPlugin : IClientServicePlugin
    {
        private readonly FtpClient ftpClient;
        private readonly IClientInfoCaching clientInfoCaching;
        public FtpPlugin(FtpClient ftpClient, IClientInfoCaching clientInfoCaching)
        {
            this.ftpClient = ftpClient;
            this.clientInfoCaching = clientInfoCaching;
        }

        public SpecialFolderInfo LocalSpecialList(ClientServicePluginExcuteWrap arg)
        {
            return ftpClient.GetSpecialFolders();
        }
        public IEnumerable<FileInfo> LocalList(ClientServicePluginExcuteWrap arg)
        {
            return ftpClient.LocalList(arg.Content);
        }
        public void SetLocalPath(ClientServicePluginExcuteWrap arg)
        {
            ftpClient.SetCurrentPath(arg.Content);
        }

        public async Task<FileInfo[]> RemoteList(ClientServicePluginExcuteWrap arg)
        {
            RemoteListModel model = arg.Content.DeJson<RemoteListModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = await ftpClient.RemoteList(model.Path, client.Socket);
                if (string.IsNullOrWhiteSpace(res.ErrorMsg))
                {
                    return res.Data;
                }
                else
                {
                    arg.SetCode(-1, res.ErrorMsg);
                }
            }
            return Array.Empty<FileInfo>();
        }
        public async Task Download(ClientServicePluginExcuteWrap arg)
        {
            RemoteDownloadModel model = arg.Content.DeJson<RemoteDownloadModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = await ftpClient.Download(model.Path, client.Socket);
                if (!res.Data)
                {
                    arg.SetCode(-1, res.ErrorMsg);
                }
            }
        }
        public void Upload(ClientServicePluginExcuteWrap arg)
        {
            RemoteUploadModel model = arg.Content.DeJson<RemoteUploadModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                ftpClient.Upload(model.Path, client.Socket);
            }
        }
        public async Task Delete(ClientServicePluginExcuteWrap arg)
        {
            RemoteDeleteModel model = arg.Content.DeJson<RemoteDeleteModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = await ftpClient.RemoteDelete(model.Path, client.Socket);
                if (!res.Data)
                {
                    arg.SetCode(-1, res.ErrorMsg);
                }
            }
        }
        public async Task Create(ClientServicePluginExcuteWrap arg)
        {
            RemoteDeleteModel model = arg.Content.DeJson<RemoteDeleteModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = await ftpClient.RemoteCreate(model.Path, client.Socket);
                if (!res.Data)
                {
                    arg.SetCode(-1, res.ErrorMsg);
                }
            }
        }
    }

    public class RemoteDeleteModel : RemoteListModel
    {
    }

    public class RemoteUploadModel : RemoteListModel
    {
    }

    public class RemoteDownloadModel : RemoteListModel
    {
    }

    public class RemoteListModel
    {
        public long Id { get; set; }
        public string Path { get; set; }
    }

    public class FtpSettingPlugin : IClientServiceSettingPlugin
    {
        private readonly Config config;
        public FtpSettingPlugin(Config config)
        {
            this.config = config;
        }

        public string Name => "文件服务";

        public string Author => "snltty";

        public string Desc => "文件上传下载服务";

        public object LoadSetting()
        {
            return config;
        }

        public string SaveSetting(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();

            config.Password = _config.Password;
            config.ServerRoot = _config.ServerRoot;
            config.ServerCurrentPath = config.ServerRoot;
            config.SaveConfig();

            return string.Empty;
        }
    }

    public class FtpPushMsgPlugin : IClientServerPushMsgPlugin
    {
        private readonly FtpClient ftpClient;
        public FtpPushMsgPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public object Info()
        {
            return new
            {
                Uploads = ftpClient.Uploads.Values,
                Downloads = ftpClient.Downloads.Values,
            };
        }
    }
}
