using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
using server.model;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.fileserver
{
    public class FileServerEventHandles
    {
        private readonly RegisterState registerState;
        private readonly IServerRequest serverRequest;

        public FileServerEventHandles(RegisterState registerState, IServerRequest serverRequest)
        {
            this.registerState = registerState;
            this.serverRequest = serverRequest;
        }

        /// <summary>
        /// 文件进度
        /// </summary>
        public SimplePushSubHandler<TcpEventArg<FileServerProgressModel>> OnTcpProgress { get; } = new SimplePushSubHandler<TcpEventArg<FileServerProgressModel>>();
        
        /// <summary>
        /// 发送文件进度
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpProgress(SendTcpEventArg<FileServerProgressModel> arg)
        {
            serverRequest.SendOnlyTcp(new plugins.serverPlugins.SendTcpEventArg<FileServerModel>
            {
                Socket = arg.Socket,
                Path = "fileserver/progress",
                Data = new FileServerModel
                {
                    FormId = registerState.RemoteInfo.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ToBytes()
                }
            });
        }

        /// <summary>
        /// 收到文件
        /// </summary>
        public SimplePushSubHandler<TcpEventArg<FileModel>> OnTcpFile { get; } = new SimplePushSubHandler<TcpEventArg<FileModel>>();

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
                    Type = FileTypes.UPLOAD
                }
            }, file);
        }


        /// <summary>
        /// 请求下载
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpDownload(SendTcpEventArg<FileServerDownloadModel> arg)
        {
            serverRequest.SendOnlyTcp(new plugins.serverPlugins.SendTcpEventArg<FileServerModel>
            {
                Socket = arg.Socket,
                Path = "fileserver/download",
                Data = new FileServerModel
                {
                    FormId = registerState.RemoteInfo.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ToBytes(),
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
                    Type = FileTypes.DOWNLOAD
                }
            }, file);
        }

        public SimplePushSubHandler<TcpEventArg<FileServerDownloadModel>> OnTcpDownload { get; } = new SimplePushSubHandler<TcpEventArg<FileServerDownloadModel>>();
        /// <summary>
        /// 请求文件列表
        /// </summary>
        /// <param name="arg"></param>
        public async Task<FileInfo[]> SendTcpFileList(SendTcpEventArg<FileServerListModel> arg)
        {
            var result = await serverRequest.SendReplyTcp(new plugins.serverPlugins.SendTcpEventArg<FileServerModel>
            {
                Socket = arg.Socket,
                Path = "fileserver/list",
                Data = new FileServerModel
                {
                    FormId = registerState.RemoteInfo.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ToBytes()
                }
            });
            if (result.Code == ServerMessageResponeCodes.OK)
            {
                return result.Data.DeBytes<FileInfo[]>();
            }
            return Array.Empty<FileInfo>();
        }

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
                });
            }
        }
        private void _SendTcpFile(SendTcpEventArg<FileModel> arg)
        {
            serverRequest.SendOnlyTcp(new plugins.serverPlugins.SendTcpEventArg<FileServerModel>
            {
                Socket = arg.Socket,
                Path = "fileserver/file",
                Data = new FileServerModel
                {
                    FormId = registerState.RemoteInfo.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ToBytes()
                }
            });
        }
    }

    public class TcpEventArg<T> : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public FileServerModel RawData { get; set; }
        public T Data { get; set; }
    }

    public class TcpFileMessageEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public FileServerModel Data { get; set; }
    }

    public class SendTcpEventArg<T> : EventArgs
    {
        public Socket Socket { get; set; }
        public T Data { get; set; }
        public long ToId { get; set; } = 0;
    }
}
