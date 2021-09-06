using client.service.events;
using client.service.p2pPlugins.plugins.request;
using common;
using common.extends;
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
        private static readonly Lazy<FileServerEventHandles> lazy = new Lazy<FileServerEventHandles>(() => new FileServerEventHandles());
        public static FileServerEventHandles Instance => lazy.Value;
        private readonly Dictionary<P2PFileCmdTypes, IP2PFilePlugin[]> plugins = null;


        private FileServerEventHandles()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IP2PFilePlugin)))
                .Select(c => (IP2PFilePlugin)Activator.CreateInstance(c)).GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public event EventHandler<TcpFileMessageEventArg> OnTcpFileMessageHandler;
        public void OnTcpFileMessage(TcpFileMessageEventArg arg)
        {
            if (plugins.ContainsKey(arg.Data.CmdType))
            {
                IP2PFilePlugin[] plugin = plugins[arg.Data.CmdType];
                if (plugin.Length > 0)
                {
                    for (int i = 0; i < plugin.Length; i++)
                    {
                        plugin[i].Excute(arg);
                    }
                }
            }

            OnTcpFileMessageHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 文件进度
        /// </summary>
        public event EventHandler<TcpFileProgressMessageEventArg> OnTcpFileProgressMessageHandler;
        public void OnTcpFileProgressMessage(TcpFileProgressMessageEventArg arg)
        {
            OnTcpFileProgressMessageHandler?.Invoke(this, arg);
        }
        /// <summary>
        /// 发送文件进度
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpFileProgressMessage(SendTcpFileMessageEventArg<P2PFileProgressModel> arg)
        {
            P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
            {
                Socket = arg.Socket,
                Data = new P2PFileModel
                {
                    CmdType = P2PFileCmdTypes.PROGRESS,
                    FormId = EventHandlers.Instance.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ProtobufSerialize()
                }
            });
        }

        /// <summary>
        /// 收到文件
        /// </summary>
        public event EventHandler<TcpFileFileMessageEventArg> OnTcpFileFileMessageHandler;
        public void OnTcpFileFileMessage(TcpFileFileMessageEventArg arg)
        {
            OnTcpFileFileMessageHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpFileUploadMessage(SendTcpFileMessageEventArg<string> arg,System.IO.FileInfo file)
        {
            SendTcpFileFileMessage(new SendTcpFileMessageEventArg<P2PFileFileModel>
            {
                ToId = arg.ToId,
                Socket = arg.Socket,
                Data = new P2PFileFileModel
                {
                    Name = file.Name,
                    Size = file.Length,
                    Md5 = arg.Data,
                    Data = Array.Empty<byte>(),
                    FileType = P2PFileCmdTypes.UPLOAD
                }
            }, file);
        }


        /// <summary>
        /// 请求下载
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpFileCmdDownloadMessage(SendTcpFileMessageEventArg<P2PFileCmdDownloadModel> arg)
        {
            P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
            {
                Socket = arg.Socket,
                Data = new P2PFileModel
                {
                    CmdType = P2PFileCmdTypes.DOWNLOAD,
                    FormId = EventHandlers.Instance.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ProtobufSerialize()
                }
            });
        }

        /// <summary>
        /// 发送对方请求下载的文件
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="file"></param>
        public void SendTcpFileDownloadMessage(SendTcpFileMessageEventArg<string> arg, System.IO.FileInfo file)
        {
            SendTcpFileFileMessage(new SendTcpFileMessageEventArg<P2PFileFileModel>
            {
                ToId = arg.ToId,
                Socket = arg.Socket,
                Data = new P2PFileFileModel
                {
                    Name = file.Name,
                    Size = file.Length,
                    Md5 = arg.Data,
                    Data = Array.Empty<byte>(),
                    FileType = P2PFileCmdTypes.DOWNLOAD
                }
            }, file);
        }
        public event EventHandler<TcpFileDownloadMessageEventArg> OnTcpFileDownloadMessageHandler;
        /// <summary>
        /// 有人请求下载
        /// </summary>
        /// <param name="arg"></param>
        public void OnTcpFileDownloadMessage(TcpFileDownloadMessageEventArg arg)
        {
            OnTcpFileDownloadMessageHandler?.Invoke(this, arg);
        }
        /// <summary>
        /// 请求文件列表
        /// </summary>
        /// <param name="arg"></param>
        public void RequestFileListMessage(SendTcpFileMessageEventArg<P2PFileCmdListModel> arg, Action<FileInfo[]> callback, Action<string> failCallback)
        {
            RequestEventHandlers.Instance.SendTcpRequestMsessage(arg.Socket, "filerequest/list", arg.Data, (msg) =>
            {
                callback(msg.Data.ProtobufDeserialize<FileInfo[]>());
            }, (msg) =>
            {
                failCallback(msg.Msg);
            });
        }

        public event EventHandler<SendTcpFileMessageEventArg<P2PFileFileModel>> OnSendTcpFileFileMessageHandler;
        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="file"></param>
        public void SendTcpFileFileMessage(SendTcpFileMessageEventArg<P2PFileFileModel> arg, System.IO.FileInfo file = null)
        {
            if (file == null)
            {
                _SendTcpChatFileMessage(arg);
                OnSendTcpFileFileMessageHandler?.Invoke(this, arg);
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
                            _SendTcpChatFileMessage(arg);
                        }
                        if (lastPackSize > 0)
                        {
                            byte[] data = new byte[lastPackSize];
                            fs.Read(data, 0, (int)lastPackSize);
                            arg.Data.Data = data;
                            _SendTcpChatFileMessage(arg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Info(ex + "");
                    }
                    OnSendTcpFileFileMessageHandler?.Invoke(this, arg);
                });
            }
        }
        private void _SendTcpChatFileMessage(SendTcpFileMessageEventArg<P2PFileFileModel> arg)
        {
            P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
            {
                Socket = arg.Socket,
                Data = new P2PFileModel
                {
                    CmdType = arg.Data.CmdType,
                    FormId = EventHandlers.Instance.ConnectId,
                    ToId = arg.ToId,
                    Data = arg.Data.ProtobufSerialize()
                }
            });
        }


    }

    public class TcpFileDownloadMessageEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public P2PFileModel RawData { get; set; }
        public P2PFileCmdDownloadModel Data { get; set; }
    }

    public class TcpFileFileMessageEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public P2PFileModel RawData { get; set; }
        public P2PFileFileModel Data { get; set; }
    }

    public class TcpFileProgressMessageEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public P2PFileModel RawData { get; set; }
        public P2PFileProgressModel Data { get; set; }
    }

    public class TcpFileMessageEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public P2PFileModel Data { get; set; }
    }

    public class SendTcpFileMessageEventArg<T> : EventArgs
    {
        public Socket Socket { get; set; }
        public T Data { get; set; }
        public long ToId { get; set; } = 0;
    }
}
