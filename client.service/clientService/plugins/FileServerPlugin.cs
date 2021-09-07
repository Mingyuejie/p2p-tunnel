using client.service.config;
using client.service.p2pPlugins.plugins.fileServer;
using client.service.serverPlugins.clients;
using common;
using common.extends;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.clientService.plugins
{

    public class FileServerPlugin : IClientServicePlugin
    {
        public FileServerConfig Info(ClientServicePluginExcuteWrap arg)
        {
            return AppShareData.Instance.FileServerConfig;
        }

        public void Update(ClientServicePluginExcuteWrap arg)
        {
            FileServerConfig model = arg.Content.DeJson<FileServerConfig>();
            if (!System.IO.Directory.Exists(model.Root))
            {
                arg.SetCode(-1, "目录不存在");
            }
            else
            {
                AppShareData.Instance.FileServerConfig = model;
                AppShareData.Instance.SaveConfig();
            }
        }

        public void Start(ClientServicePluginExcuteWrap arg)
        {
            FileServerHelper.Instance.Start();
            AppShareData.Instance.SaveConfig();
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            FileServerHelper.Instance.Stop();
            AppShareData.Instance.SaveConfig();
        }

        public void Download(ClientServicePluginExcuteWrap arg)
        {
            RequestFileDownloadModel model = arg.Content.DeJson<RequestFileDownloadModel>();
            if (!FileServerHelper.Instance.Download(model.ToId, model.Path))
            {
                arg.SetCode(-1, "请选择目标对象");
            }
        }

        public void Upload(ClientServicePluginExcuteWrap arg)
        {
            RequestFileUploadModel model = arg.Content.DeJson<RequestFileUploadModel>();
            if (!FileServerHelper.Instance.Upload(model.ToId, model.Path))
            {
                arg.SetCode(-1, "请选择目标对象");
            }
        }

        public async Task<FileInfo[]> List(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = arg.Content.DeJson<RequestFileListModel>();
            var result = await FileServerHelper.Instance.RequestRemoteList(model.ToId, model.Path);
            if (!string.IsNullOrWhiteSpace(result.ErrorMsg))
            {
                arg.SetCode(-1, result.ErrorMsg);
                return Array.Empty<FileInfo>();
            }

            return result.Data;
        }

        public FileInfo[] LocalList(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = arg.Content.DeJson<RequestFileListModel>();
            return FileServerHelper.Instance.GetLocalFiles(model.Path, model.ToId == -1);
        }

        public IEnumerable<object> Online(ClientServicePluginExcuteWrap arg)
        {
            return FileServerHelper.Instance.GetOnlineList();
        }

        public SpecialFolderInfo SpecialFolder(ClientServicePluginExcuteWrap arg)
        {
            return FileServerHelper.Instance.GetSpecialFolders();
        }
    }

    public class RequestFileListModel
    {
        public long ToId { get; set; } = 0;

        public string Path { get; set; } = string.Empty;
    }

    public class RequestFileDownloadModel
    {
        public long ToId { get; set; } = 0;

        public string Path { get; set; } = string.Empty;
    }

    public class RequestFileUploadModel
    {
        public long ToId { get; set; } = 0;

        public string Path { get; set; } = string.Empty;
    }
}
