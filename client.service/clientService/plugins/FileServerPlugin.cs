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
                arg.SetResultCode(-1,"目录不存在");
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
            FileInfo[] result = Array.Empty<FileInfo>();
            var socket = GetSocket(model.ToId);
            if (socket != null)
            {
                FileServerEventHandles.Instance.SendTcpFileCmdDownloadMessage(new SendTcpFileMessageEventArg<P2PFileCmdDownloadModel>
                {
                    Data = new P2PFileCmdDownloadModel { Path = model.Path },
                    Socket = socket,
                    ToId = model.ToId
                });
            }
            else
            {
                arg.SetResultCode(-1, "请选择目标对象");
            }
            arg.Callback(arg, null);
        }

        public void Upload(ClientServicePluginExcuteWrap arg)
        {
            RequestFileUploadModel model = Helper.DeJsonSerializer<RequestFileUploadModel>(arg.Content);
            FileInfo[] result = Array.Empty<FileInfo>();
            var socket = GetSocket(model.ToId);
            if (socket != null)
            {
                FileServerEventHandles.Instance.SendTcpFileUploadMessage(new SendTcpFileMessageEventArg<P2PFileCmdUploadModel>
                {
                    Data = new P2PFileCmdUploadModel { Path = model.Path },
                    Socket = socket,
                    ToId = model.ToId
                });
            }
            else
            {
                arg.SetResultCode(-1, "请选择目标对象");
               
            }
            arg.Callback(arg, null);
        }

        public void List(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = Helper.DeJsonSerializer<RequestFileListModel>(arg.Content);
            var socket = GetSocket(model.ToId);
            if (socket != null)
            {
                FileServerEventHandles.Instance.RequestFileListMessage(new SendTcpFileMessageEventArg<P2PFileCmdListModel>
                {
                    ToId = model.ToId,
                    Socket = socket,
                    Data = new P2PFileCmdListModel { Path = model.Path }
                }, (msg) =>
                {
                    for (int i = 0; i < msg.Length; i++)
                    {
                        if(msg[i].Type == 0)
                        {
                            msg[i].Image = FileServerHelper.Instance.GetDirectoryIcon();
                        }
                        else
                        {
                            msg[i].Image = FileServerHelper.Instance.GetFileIcon(msg[i].Name);
                        }
                    }

                    arg.Callback(arg, msg);
                }, (msg) =>
                {
                    arg.SetResultCode(-1, msg);
                    arg.Callback(arg, Array.Empty<FileInfo>());
                });
            }
            else
            {
                arg.Callback(arg, Array.Empty<FileInfo>());
            }
        }

        public void LocalList(ClientServicePluginExcuteWrap arg)
        {
            RequestFileListModel model = Helper.DeJsonSerializer<RequestFileListModel>(arg.Content);
            arg.Callback(arg, FileServerHelper.Instance.GetLocalFiles(model.Path));
        }

        public void Online(ClientServicePluginExcuteWrap arg)
        {
            arg.Callback(arg, FileServerHelper.Instance.GetOnlineList());
        }

        private Socket GetSocket(long id)
        {
            AppShareData.Instance.Clients.TryGetValue(id, out ClientInfo client);
            return client?.Socket ?? null;
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
