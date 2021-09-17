using client.service.clientService;
using client.service.config;
using common;
using common.extends;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.p2pPlugins.fileServer.client
{

    public class FileServerPlugin : IClientServicePlugin
    {
        private readonly Config config;
        private readonly FileServerHelper fileServerHelper;
        private readonly FileServerEventHandles fileServerEventHandles;


        public FileServerPlugin(Config config, FileServerHelper fileServerHelper, FileServerEventHandles fileServerEventHandles)
        {
            this.config = config;
            this.fileServerHelper = fileServerHelper;
            this.fileServerEventHandles = fileServerEventHandles;
        }

        public FileServerConfig Info(ClientServicePluginExcuteWrap arg)
        {
            return config.FileServer;
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
                config.FileServer = model;
                config.SaveConfig();
            }
        }

        public void Start(ClientServicePluginExcuteWrap arg)
        {
            fileServerHelper.Start();
            config.SaveConfig();
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            fileServerHelper.Stop();
            config.SaveConfig();
        }

        public void Download(ClientServicePluginExcuteWrap arg)
        {
            RequestFileDownloadModel model = arg.Content.DeJson<RequestFileDownloadModel>();
            if (!fileServerHelper.Download(model.ToId, model.Path))
            {
                arg.SetCode(-1, "请选择目标对象");
            }
        }

        public void Upload(ClientServicePluginExcuteWrap arg)
        {
            RequestFileUploadModel model = arg.Content.DeJson<RequestFileUploadModel>();
            if (!fileServerHelper.Upload(model.ToId, model.Path))
            {
                arg.SetCode(-1, "请选择目标对象");
            }
        }

        public FileInfo[] List(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = arg.Content.DeJson<RequestFileListModel>();
            var files = fileServerHelper.SendTcpFileList(new SendTcpEventArg<FileServerListModel>
            {
                ToId = model.ToId,
                Data = new FileServerListModel { Path = model.Path }
            }).Result;
            return files;
        }

        public FileInfo[] LocalList(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = arg.Content.DeJson<RequestFileListModel>();
            return fileServerHelper.GetLocalFiles(model.Path, model.ToId == -1);
        }

        public IEnumerable<object> Online(ClientServicePluginExcuteWrap arg)
        {
            return fileServerHelper.GetOnlineList();
        }

        public SpecialFolderInfo SpecialFolder(ClientServicePluginExcuteWrap arg)
        {
            return fileServerHelper.GetSpecialFolders();
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
