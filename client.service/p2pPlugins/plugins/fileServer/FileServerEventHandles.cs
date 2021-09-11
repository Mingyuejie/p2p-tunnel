using client.service.events;
using client.service.p2pPlugins.plugins.request;
using client.service.serverPlugins.register;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public class FileServerEventHandles
    {
        private Dictionary<FileServerCmdTypes, IFileServerPlugin[]> plugins = null;

        private readonly P2PEventHandles p2PEventHandles;
        private readonly RequestEventHandlers requestEventHandlers;
        private readonly RegisterState registerState;

        public FileServerEventHandles(P2PEventHandles p2PEventHandles, RequestEventHandlers requestEventHandlers, RegisterState registerState)
        {
            this.p2PEventHandles = p2PEventHandles;
            this.requestEventHandlers = requestEventHandlers;
            this.registerState = registerState;

           
        }

        public void LoadPlugins()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(c => c.GetTypes())
               .Where(c => c.GetInterfaces().Contains(typeof(IFileServerPlugin)))
               .Select(c => (IFileServerPlugin)Program.serviceProvider.GetService(c)).GroupBy(c => c.Type)
               .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public event EventHandler<TcpFileMessageEventArg> OnTcpFileServerHandler;
        public void OnTcpFileServer(TcpFileMessageEventArg arg)
        {
            if (plugins.ContainsKey(arg.Data.CmdType))
            {
                IFileServerPlugin[] plugin = plugins[arg.Data.CmdType];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnTcpFileServerHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 文件进度
        /// </summary>
        public event EventHandler<TcpEventArg<FileServerProgressModel>> OnTcpProgressHandler;
        public void OnTcpProgress(TcpEventArg<FileServerProgressModel> arg)
        {
            OnTcpProgressHandler?.Invoke(this, arg);
        }
        /// <summary>
        /// 发送文件进度
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpProgress(SendTcpEventArg<FileServerProgressModel> arg)
        {
            p2PEventHandles.SendTcp(new SendP2PTcpArg
            {
                Socket = arg.Socket,
                Data = new FileServerModel
                {
                    CmdType = FileServerCmdTypes.PROGRESS,
                    FormId = registerState.RemoteInfo.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ToBytes()
                }
            });
        }

        /// <summary>
        /// 收到文件
        /// </summary>
        public event EventHandler<TcpEventArg<FileModel>> OnTcpFileHandler;
        public void OnTcpFile(TcpEventArg<FileModel> arg)
        {
            OnTcpFileHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpUpload(SendTcpEventArg<string> arg, System.IO.FileInfo file)
        {
            SendTcpFile(new SendTcpEventArg<FileModel>
            {
                ToId = arg.ToId,
                Socket = arg.Socket,
                Data = new FileModel
                {
                    Name = file.Name,
                    Size = file.Length,
                    Md5 = arg.Data,
                    Data = Array.Empty<byte>(),
                    FileType = FileServerCmdTypes.UPLOAD
                }
            }, file);
        }


        /// <summary>
        /// 请求下载
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpDownload(SendTcpEventArg<FileServerDownloadModel> arg)
        {
            p2PEventHandles.SendTcp(new SendP2PTcpArg
            {
                Socket = arg.Socket,
                Data = new FileServerModel
                {
                    CmdType = FileServerCmdTypes.DOWNLOAD,
                    FormId = registerState.RemoteInfo.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ToBytes()
                }
            });
        }

        /// <summary>
        /// 发送对方请求下载的文件
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="file"></param>
        public void SendTcpDownload(SendTcpEventArg<string> arg, System.IO.FileInfo file)
        {
            SendTcpFile(new SendTcpEventArg<FileModel>
            {
                ToId = arg.ToId,
                Socket = arg.Socket,
                Data = new FileModel
                {
                    Name = file.Name,
                    Size = file.Length,
                    Md5 = arg.Data,
                    Data = Array.Empty<byte>(),
                    FileType = FileServerCmdTypes.DOWNLOAD
                }
            }, file);
        }
        public event EventHandler<TcpEventArg<FileServerDownloadModel>> OnTcpDownloadHandler;
        /// <summary>
        /// 有人请求下载
        /// </summary>
        /// <param name="arg"></param>
        public void OnTcpDownload(TcpEventArg<FileServerDownloadModel> arg)
        {
            OnTcpDownloadHandler?.Invoke(this, arg);
        }
        /// <summary>
        /// 请求文件列表
        /// </summary>
        /// <param name="arg"></param>
        public async Task<CommonTaskResponseModel<FileInfo[]>> RequestTcpFileList(SendTcpEventArg<FileServerListModel> arg)
        {
            var result = await requestEventHandlers.TcpRequest(new TcpRequestParam
            {
                Socket = arg.Socket,
                Path = "filerequest/list",
                Data = arg.Data
            });
            if (result.Code == MessageRequestResponseCodes.OK)
            {
                return new CommonTaskResponseModel<FileInfo[]> { Data = result.Data.DeBytes<FileInfo[]>() };
            }
            return new CommonTaskResponseModel<FileInfo[]> { ErrorMsg = result.ErrorMsg };
        }

        public event EventHandler<SendTcpEventArg<FileModel>> OnSendTcpFileHandler;
        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="file"></param>
        public void SendTcpFile(SendTcpEventArg<FileModel> arg, System.IO.FileInfo file = null)
        {
            if (file == null)
            {
                _SendTcpFile(arg);
                OnSendTcpFileHandler?.Invoke(this, arg);
            }
            else
            {
                //文件分包传输
                Task.Run(() =>
                {
                    try
                    {
                        int packSize = 1024; //每个包大小 

                        int packCount = (int)(arg.Data.Size / packSize);
                        long lastPackSize = arg.Data.Size - packCount * packSize;
                        int index = 0;
                        using FileStream fs = file.OpenRead();
                        for (index = 0; index < packCount; index++)
                        {
                            byte[] data = new byte[packSize];
                            fs.Read(data, 0, packSize);

                            arg.Data.Data = data;
                            _SendTcpFile(arg);
                        }
                        if (lastPackSize > 0)
                        {
                            byte[] data = new byte[lastPackSize];
                            fs.Read(data, 0, (int)lastPackSize);
                            arg.Data.Data = data;
                            _SendTcpFile(arg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Debug(ex + "");
                    }
                    OnSendTcpFileHandler?.Invoke(this, arg);
                });
            }
        }
        private void _SendTcpFile(SendTcpEventArg<FileModel> arg)
        {
            p2PEventHandles.SendTcp(new SendP2PTcpArg
            {
                Socket = arg.Socket,
                Data = new FileServerModel
                {
                    CmdType = arg.Data.CmdType,
                    FormId = registerState.RemoteInfo.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ToBytes()
                }
            });
        }

    }

    public class TcpEventArg<T> : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public FileServerModel RawData { get; set; }
        public T Data { get; set; }
    }

    public class TcpFileMessageEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public FileServerModel Data { get; set; }
    }

    public class SendTcpEventArg<T> : EventArgs
    {
        public Socket Socket { get; set; }
        public T Data { get; set; }
        public long ToId { get; set; } = 0;
    }
}
