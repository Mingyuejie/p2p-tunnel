using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using client.service.ftp.server.plugin;
using common;
using common.extends;
using server.model;
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

        public SpecialFolderInfo LocalSpecialList(ClientServicePluginExecuteWrap arg)
        {
            return ftpClient.GetSpecialFolders();
        }
        public object LocalList(ClientServicePluginExecuteWrap arg)
        {
            var list = ftpClient.LocalList(arg.Content);
            return new
            {
                Current = config.ClientCurrentPath,
                Data = list
            };
        }
        public void SetLocalPath(ClientServicePluginExecuteWrap arg)
        {
            ftpClient.SetCurrentPath(arg.Content);
        }
        public void LocalCreate(ClientServicePluginExecuteWrap arg)
        {
            ftpClient.Create(arg.Content);
        }
        public void LocalDelete(ClientServicePluginExecuteWrap arg)
        {
            ftpClient.Delete(arg.Content);
        }
        public async Task LocalCancel(ClientServicePluginExecuteWrap arg)
        {
            RemoteCancelModel model = arg.Content.DeJson<RemoteCancelModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                await ftpClient.OnFileUploadCancel(new protocol.FtpCancelCommand { Md5 = model.Md5 }, client);
            }
        }

        public async Task<bool> RemoteCancel(ClientServicePluginExecuteWrap arg)
        {
            RemoteCancelModel model = arg.Content.DeJson<RemoteCancelModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                var res = await ftpClient.RemoteCancel(model.Md5, client);
                if (res.Code != FtpResultModel.FtpResultCodes.OK)
                {
                    arg.SetErrorMessage(res.Code.GetDesc((byte)res.Code));
                }
            }
            return false;

        }
        public async Task<FileInfo[]> RemoteList(ClientServicePluginExecuteWrap arg)
        {
            RemoteListModel model = arg.Content.DeJson<RemoteListModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                return await ftpClient.RemoteList(model.Path, client);
            }
            return Array.Empty<FileInfo>();
        }
        public async Task<bool> Download(ClientServicePluginExecuteWrap arg)
        {
            RemoteDownloadModel model = arg.Content.DeJson<RemoteDownloadModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                return await ftpClient.Download(model.Path, client);
            }
            return false;
        }
        public async Task Upload(ClientServicePluginExecuteWrap arg)
        {
            RemoteUploadModel model = arg.Content.DeJson<RemoteUploadModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                await ftpClient.Upload(model.Path, client);
            }
        }
        public async Task<bool> RemoteDelete(ClientServicePluginExecuteWrap arg)
        {
            RemoteDeleteModel model = arg.Content.DeJson<RemoteDeleteModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                var res = await ftpClient.RemoteDelete(model.Path, client);
                if (res.Code != FtpResultModel.FtpResultCodes.OK)
                {
                    arg.SetErrorMessage(res.Code.GetDesc((byte)res.Code));
                }
            }
            return false;
        }
        public async Task<bool> RemoteCreate(ClientServicePluginExecuteWrap arg)
        {
            RemoteDeleteModel model = arg.Content.DeJson<RemoteDeleteModel>();
            if (clientInfoCaching.Get(model.Id, out ClientInfo client))
            {
                var res = await ftpClient.RemoteCreate(model.Path, client);
                if (res.Code != FtpResultModel.FtpResultCodes.OK)
                {
                    arg.SetErrorMessage(res.Code.GetDesc((byte)res.Code));
                }
            }
            return false;
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
        public ulong Id { get; set; }
        public ulong Md5 { get; set; }
    }
    public class RemoteListModel
    {
        public ulong Id { get; set; }
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

        public bool Enable => config.Enable;

        public async Task<object> LoadSetting()
        {
            return await Task.FromResult(config);
        }

        public async Task<string> SaveSetting(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();

            config.Password = _config.Password;
            config.ServerRoot = _config.ServerRoot;
            config.ServerCurrentPath = config.ServerRoot;
            config.Enable = _config.Enable;
            config.UploadNum = _config.UploadNum;
            await config.SaveConfig();

            return string.Empty;
        }

        public async Task<bool> SwitchEnable(bool enable)
        {
            config.Enable = enable;
            await config.SaveConfig();
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
