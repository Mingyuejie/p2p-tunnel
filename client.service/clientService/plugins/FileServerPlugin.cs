using client.service.config;
using client.service.p2pPlugins.plugins.fileServer;
using client.service.serverPlugins.clients;
using common;
using System;
using System.Net.Sockets;

namespace client.service.clientService.plugins
{

    public class FileServerPlugin : IClientServicePlugin
    {
        public void Info(ClientServicePluginExcuteWrap arg)
        {
            arg.Callback(arg, AppShareData.Instance.FileServerConfig);
        }

        public void Update(ClientServicePluginExcuteWrap arg)
        {
            FileServerConfig model = Helper.DeJsonSerializer<FileServerConfig>(arg.Content);
            if (!System.IO.Directory.Exists(model.Root))
            {
                arg.SetResultCode(-1, "目录不存在");
            }
            else
            {
                AppShareData.Instance.FileServerConfig = model;
                AppShareData.Instance.SaveConfig();
            }
            arg.Callback(arg, null);
        }

        public void Start(ClientServicePluginExcuteWrap arg)
        {
            FileServerHelper.Instance.Start();
            AppShareData.Instance.SaveConfig();
            arg.Callback(arg, null);
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            FileServerHelper.Instance.Stop();
            AppShareData.Instance.SaveConfig();
            arg.Callback(arg, null);
        }

        public void Download(ClientServicePluginExcuteWrap arg)
        {
            RequestFileDownloadModel model = Helper.DeJsonSerializer<RequestFileDownloadModel>(arg.Content);
            if (!FileServerHelper.Instance.Download(model.ToId, model.Path))
            {
                arg.SetResultCode(-1, "请选择目标对象");
            }
            arg.Callback(arg, null);
        }

        public void Upload(ClientServicePluginExcuteWrap arg)
        {
            RequestFileUploadModel model = Helper.DeJsonSerializer<RequestFileUploadModel>(arg.Content);
            if (!FileServerHelper.Instance.Upload(model.ToId, model.Path))
            {
                arg.SetResultCode(-1, "请选择目标对象");
            }
            arg.Callback(arg, null);
        }

        public void List(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = Helper.DeJsonSerializer<RequestFileListModel>(arg.Content);
            if (!FileServerHelper.Instance.RequestRemoteList(model.ToId, model.Path, (msg) =>
            {
                arg.Callback(arg, msg);
            }, (msg) =>
            {
                arg.SetResultCode(-1, msg);
                arg.Callback(arg, Array.Empty<FileInfo>());
            }))
            {
                arg.SetResultCode(-1, "请选择目标对象");
                arg.Callback(arg, Array.Empty<FileInfo>());
            }
        }

        public void LocalList(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = Helper.DeJsonSerializer<RequestFileListModel>(arg.Content);
            arg.Callback(arg, FileServerHelper.Instance.GetLocalFiles(model.Path, model.ToId == -1));
        }

        public void Online(ClientServicePluginExcuteWrap arg)
        {
            arg.Callback(arg, FileServerHelper.Instance.GetOnlineList());
        }

        public void SpecialFolder(ClientServicePluginExcuteWrap arg)
        {
            arg.Callback(arg, FileServerHelper.Instance.GetSpecialFolders());
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
