using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using client.service.ftp.server.plugin;
using common;
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
        private readonly Config config;
        public FtpPlugin(FtpClient ftpClient, IClientInfoCaching clientInfoCaching, Config config)
        {
            this.ftpClient = ftpClient;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
        }

        public SpecialFolderInfo LocalSpecialList(ClientServicePluginExcuteWrap arg)
        {
            return ftpClient.GetSpecialFolders();
        }
        public object LocalList(ClientServicePluginExcuteWrap arg)
        {
            var list = ftpClient.LocalList(arg.Content);
            return new
            {
                Current = config.ClientCurrentPath,
                Data = list
            };
        }
        public void SetLocalPath(ClientServicePluginExcuteWrap arg)
        {
            ftpClient.SetCurrentPath(arg.Content);
        }
        public void LocalCreate(ClientServicePluginExcuteWrap arg)
        {
            ftpClient.Create(arg.Content);
        }
        public void LocalDelete(ClientServicePluginExcuteWrap arg)
        {
            ftpClient.Delete(arg.Content);
        }
        public void LocalCancel(ClientServicePluginExcuteWrap arg)
        {
            RemoteCancelModel model = arg.Content.DeJson<RemoteCancelModel>();
            ftpClient.OnFileUploadCancel(new protocol.FtpCancelCommand { Md5 = model.Md5, SessionId = model.Id });
        }

        public bool RemoteCancel(ClientServicePluginExcuteWrap arg)
        {
            RemoteCancelModel model = arg.Content.DeJson<RemoteCancelModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                return ftpClient.RemoteCancel(model.Md5, client).Result.Data;
            }
            return false;

        }
        public async Task<FileInfo[]> RemoteList(ClientServicePluginExcuteWrap arg)
        {
            RemoteListModel model = arg.Content.DeJson<RemoteListModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = await ftpClient.RemoteList(model.Path, client);
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
                var res = await ftpClient.Download(model.Path, client);
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
                ftpClient.Upload(model.Path, client);
            }
        }
        public async Task RemoteDelete(ClientServicePluginExcuteWrap arg)
        {
            RemoteDeleteModel model = arg.Content.DeJson<RemoteDeleteModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = await ftpClient.RemoteDelete(model.Path, client);
                if (!res.Data)
                {
                    arg.SetCode(-1, res.ErrorMsg);
                }
            }
        }
        public async Task RemoteCreate(ClientServicePluginExcuteWrap arg)
        {
            RemoteDeleteModel model = arg.Content.DeJson<RemoteDeleteModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client) && client != null && client.TcpConnected)
            {
                var res = await ftpClient.RemoteCreate(model.Path, client);
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
    public class RemoteCancelModel
    {
        public long Id { get; set; }
        public long Md5 { get; set; }
    }
    public class RemoteListModel
    {
        public long Id { get; set; }
        public string Path { get; set; }
    }
    public class RemoteSessionModel
    {
        public long Id { get; set; }
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

        public bool Enable => config.Enable;

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
            config.Enable = _config.Enable;
            config.UploadNum = _config.UploadNum;
            config.SaveConfig();

            return string.Empty;
        }

        public bool SwitchEnable(bool enable)
        {
            config.Enable = enable;
            config.SaveConfig();
            return true;
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
                Uploads = ftpClient.Uploads.Caches.Values.SelectMany(c => c.Values),
                Downloads = ftpClient.Downloads.Caches.Values.SelectMany(c => c.Values),
            };
        }
    }
}
