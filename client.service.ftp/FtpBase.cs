using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.service.ftp.extends;
using client.service.ftp.protocol;
using common;
using common.extends;
using MessagePack;
using ProtoBuf;
using server;
using server.model;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.ftp
{
    public class FtpBase
    {
        public FileSaveManager Downloads { get; } = new();
        public FileSaveManager Uploads { get; } = new();
        protected virtual string SocketPath { get; set; }
        protected virtual string RootPath { get; set; }

        private NumberSpace fileIdNs = new NumberSpace(0);

        protected readonly IServerRequest serverRequest;
        protected readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        public FtpBase(IServerRequest serverRequest, Config config, IClientInfoCaching clientInfoCaching)
        {
            this.serverRequest = serverRequest;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                Downloads.Clear(client.Id);
                Uploads.Clear(client.Id);
            });

            LoopProgress();
        }

        protected List<string> Create(string currentPath, string path)
        {
            return path.CreateDir(currentPath, RootPath);
        }
        protected List<string> Delete(string currentPath, string path)
        {
            return path.ClearDir(currentPath, RootPath);
        }
        protected async Task Upload(string currentPath, string path, ClientInfo client)
        {
            await Task.Run(async () =>
            {
                foreach (string item in path.Split(Helper.SeparatorChar))
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        string filepath = Path.Combine(currentPath, item);
                        if (Directory.Exists(filepath))
                        {
                            List<FileUploadInfo> files = new();
                            GetFiles(files, new DirectoryInfo(filepath));

                            IEnumerable<string> paths = files.Where(c => c.Type == FileType.Folder)
                                .Select(c => c.Path.Replace(currentPath, String.Empty).TrimStart(Path.DirectorySeparatorChar));
                            if (paths.Any())
                            {
                                await RemoteCreate(string.Join(Helper.SeparatorString, paths), client);
                            }
                            foreach (FileUploadInfo file in files.Where(c => c.Type == FileType.File))
                            {
                                AppendUpload(file.Path, currentPath, client);
                            }
                        }
                        else if (File.Exists(filepath))
                        {
                            AppendUpload(filepath, currentPath, client);
                        }
                    }
                }
            });
        }
        private void AppendUpload(string path, string currentPath, ClientInfo client)
        {
            var file = new System.IO.FileInfo(path);
            try
            {
                //同时上传重复的文件
                if (Uploads.Contains(client.Id, file.FullName))
                {
                    return;
                }

                var save = new FileSaveInfo
                {
                    FileName = file.FullName,
                    IndexLength = 0,
                    TotalLength = file.Length,
                    Md5 = fileIdNs.Get(),
                    ClientId = client.Id,
                    State = UploadState.Wait,
                    CacheFileName = file.FullName.Replace(currentPath, String.Empty).TrimStart(Path.DirectorySeparatorChar)
                };
                Uploads.Add(save);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($" Upload {ex}");
            }
        }
        private void Upload(FileSaveInfo save)
        {
            if (save == null) return;

            clientInfoCaching.Get(save.ClientId, out ClientInfo client);

            FtpFileCommand cmd = new FtpFileCommand
            {
                Md5 = save.Md5,
                Size = save.TotalLength,
                Name = save.CacheFileName
            };
            save.State = UploadState.Uploading;
            int packSize = config.SendPacketSize; //每个包大小
            int packCount = (int)(save.TotalLength / packSize);
            int lastPackSize = (int)(save.TotalLength - (packCount * packSize));

            Task.Run(async () =>
            {
                try
                {
                    cmd.ToBytes();
                    using FileStream fs = new FileStream(save.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, config.ReadWriteBufferSize, true);

                    int index = 0;
                    while (index < packCount)
                    {
                        if (!save.Check())
                        {
                            return;
                        }

                        byte[] data = new byte[packSize];
                        await fs.ReadAsync(data, 0, packSize);
                        var sendData = cmd.WriteData(data);
                        if (!await SendOnlyTcp(sendData, client.TcpConnection))
                        {
                            save.State = UploadState.Error;
                        }
                        save.IndexLength += data.Length;

                        index++;
                    }
                    if (!save.Check())
                    {
                        return;
                    }
                    if (lastPackSize > 0)
                    {
                        byte[] data = new byte[lastPackSize];
                        await fs.ReadAsync(data, 0, lastPackSize);
                        var sendData = cmd.WriteData(data);
                        if (!await SendOnlyTcp(sendData, client.TcpConnection))
                        {
                            save.State = UploadState.Error;
                        }
                        save.IndexLength += data.Length;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex);
                    save.Disponse();
                }
            }, save.Token.Token);
        }

        protected async Task OnFile(string currentPath, FtpFileCommand cmd, FtpPluginParamWrap wrap)
        {
            try
            {
                FileSaveInfo fs = Downloads.Get(wrap.Client.Id, cmd.Md5);
                if (fs == null)
                {
                    fs = new FileSaveInfo
                    {
                        Stream = null,
                        IndexLength = 0,
                        TotalLength = cmd.Size,
                        FileName = Path.Combine(currentPath, cmd.Name),
                        CacheFileName = Path.Combine(currentPath, $"{cmd.Md5}.downloading"),
                        ClientId = wrap.Client.Id,
                        Md5 = cmd.Md5,
                        State = UploadState.Wait
                    };
                    Downloads.Add(fs);
                }
                else if (fs.Token.IsCancellationRequested || fs.State == UploadState.Canceled)
                {
                    return;
                }
                if (cmd.ReadData.Length == 0)
                {
                    return;
                }

                if (fs.Stream == null)
                {
                    fs.CacheFileName.TryDeleteFile();
                    fs.Stream = new FileStream(fs.CacheFileName, FileMode.Create & FileMode.Append, FileAccess.Write, FileShare.Read, config.ReadWriteBufferSize, true);
                    fs.Stream.Seek(cmd.Size - 1, SeekOrigin.Begin);
                    fs.Stream.WriteByte(new byte());
                    fs.Stream.Seek(0, SeekOrigin.Begin);
                }

                await fs.Stream.WriteAsync(cmd.ReadData, fs.Token.Token);
                fs.IndexLength += cmd.ReadData.Length;

                if (fs.IndexLength >= cmd.Size)
                {
                    await SendOnlyTcp(new FtpFileEndCommand { Md5 = cmd.Md5 }, wrap.Client);
                    Downloads.Remove(wrap.Client.Id, cmd.Md5);
                    File.Move(fs.CacheFileName, fs.FileName, true);
                }
            }
            catch (Exception ex)
            {
                await SendOnlyTcp(new FtpFileErrorCommand { Md5 = cmd.Md5, Msg = ex.Message }, wrap.Client);
                Downloads.Remove(wrap.Client.Id, cmd.Md5, true);
                Logger.Instance.Error(ex);
            }
        }
        public void OnFileEnd(FtpFileEndCommand cmd, FtpPluginParamWrap wrap)
        {
            Uploads.Remove(wrap.Client.Id, cmd.Md5);
        }
        public void OnFileError(FtpFileErrorCommand cmd, FtpPluginParamWrap wrap)
        {
            Uploads.Remove(wrap.Client.Id, cmd.Md5);
        }
        public async Task OnFileUploadCancel(FtpCancelCommand cmd, FtpPluginParamWrap wrap)
        {
            await OnFileUploadCancel(cmd, wrap.Client);
        }
        public async Task OnFileUploadCancel(FtpCancelCommand cmd, ClientInfo client)
        {
            Uploads.Remove(client.Id, cmd.Md5);
            await SendOnlyTcp(new FtpCanceledCommand { Md5 = cmd.Md5 }, client);
        }

        public void OnFileUploadCanceled(FtpCanceledCommand cmd, FtpPluginParamWrap wrap)
        {
            Downloads.Remove(wrap.Client.Id, cmd.Md5, true);
        }

        public async Task<FtpResultModel> RemoteCreate(string path, ClientInfo client)
        {
            MessageRequestResponeWrap resp = await SendReplyTcp(new FtpCreateCommand { Path = path }, client);
            return FtpResultModel.FromBytes(resp.Data);
        }
        public async Task<FtpResultModel> RemoteCancel(ulong md5, ClientInfo client)
        {
            MessageRequestResponeWrap resp = await SendReplyTcp(new FtpCancelCommand { Md5 = md5 }, client);
            return FtpResultModel.FromBytes(resp.Data);
        }
        public async Task<FtpResultModel> RemoteDelete(string path, ClientInfo client)
        {
            MessageRequestResponeWrap resp = await SendReplyTcp(new FtpDelCommand { Path = path }, client);
            return FtpResultModel.FromBytes(resp.Data);
        }

        protected async Task<bool> SendOnlyTcp(IFtpCommandBase data, ClientInfo client)
        {
            return await SendOnlyTcp(data, client.TcpConnection);
        }
        protected async Task<bool> SendOnlyTcp(IFtpCommandBase data, IConnection connection)
        {
            return await SendOnlyTcp(data.ToBytes(), connection);
        }
        protected async Task<bool> SendOnlyTcp(byte[] data, IConnection connection)
        {
            return await serverRequest.SendOnly(new SendEventArg<byte[]>
            {
                Data = data,
                Path = SocketPath,
                Connection = connection
            });
        }
        protected async Task<MessageRequestResponeWrap> SendReplyTcp(IFtpCommandBase data, ClientInfo client)
        {
            return await serverRequest.SendReply(new SendEventArg<byte[]>
            {
                Data = data.ToBytes(),
                Path = SocketPath,
                Connection = client.TcpConnection
            });
        }

        private void GetFiles(List<FileUploadInfo> files, DirectoryInfo path)
        {
            files.Add(new FileUploadInfo { Path = path.FullName, Type = FileType.Folder });
            foreach (var dir in path.GetDirectories())
            {
                GetFiles(files, dir);
            }
            files.AddRange(path.GetFiles().Select(c => new FileUploadInfo
            {
                Path = c.FullName,
                Type = FileType.File
            }));
        }
        private void LoopProgress()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (!Uploads.Caches.IsEmpty)
                    {
                        foreach (var item in Uploads.Caches.SelectMany(c => c.Value.Values))
                        {
                            if (item.TotalLength > 0 && item.IndexLength > 0)
                            {
                                item.Speed = (item.IndexLength - item.LastLength);
                                item.LastLength = item.IndexLength;
                            }
                        }

                        IEnumerable<FileSaveInfo> saves = Uploads.Caches.SelectMany(c => c.Value.Values);
                        int uploadCount = saves.Count(c => c.State == UploadState.Uploading);
                        int waitCount = saves.Count(c => c.State == UploadState.Wait);
                        if (waitCount > 0 && uploadCount < config.UploadNum)
                        {
                            Upload(saves.FirstOrDefault(c => c.State == UploadState.Wait));
                        }
                    }
                    if (!Downloads.Caches.IsEmpty)
                    {
                        foreach (var item in Downloads.Caches.SelectMany(c => c.Value.Values))
                        {
                            if (item.TotalLength > 0 && item.IndexLength > 0)
                            {
                                item.Speed = (item.IndexLength - item.LastLength);
                                item.LastLength = item.IndexLength;
                            }
                        }
                    }

                    await Task.Delay(1000);
                }
            }, TaskCreationOptions.LongRunning);
        }

    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FileType : byte
    {
        [Description("文件夹")]
        Folder = 0,
        [Description("文件")]
        File = 1
    }

    [ProtoContract, MessagePackObject]
    public class FileInfo
    {
        [ProtoMember(1), Key(1)]
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
        [ProtoMember(2), Key(2)]
        public DateTime CreationTime { get; set; } = DateTime.Now;
        [ProtoMember(3), Key(3)]
        public DateTime LastWriteTime { get; set; } = DateTime.Now;
        [ProtoMember(4), Key(4)]
        public string Name { get; set; } = string.Empty;
        [ProtoMember(5), Key(5)]
        public long Length { get; set; } = 0;
        [ProtoMember(6, IsRequired = true), Key(6)]
        public FileType Type { get; set; } = FileType.File;
    }

    public struct FileUploadInfo
    {
        public string Path { get; set; }
        public FileType Type { get; set; }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum UploadState : byte
    {
        [Description("等待中")]
        Wait = 0,
        [Description("上传中")]
        Uploading = 1,
        [Description("已取消")]
        Canceled = 2,
        [Description("出错")]
        Error = 3
    }

    public class FileSaveInfo
    {
        [JsonIgnore]
        public FileStream Stream { get; set; }
        [JsonIgnore]
        public ulong ClientId { get; set; }
        public ulong Md5 { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; } = 0;
        public string FileName { get; set; } = string.Empty;
        [JsonIgnore]
        public string CacheFileName { get; set; } = string.Empty;
        [JsonIgnore]
        public long LastLength { get; set; } = 0;

        [JsonIgnore]
        public CancellationTokenSource Token { get; set; }

        public long Speed { get; set; } = 0;
        public UploadState State { get; set; } = UploadState.Wait;

        public void Disponse(bool deleteFile = false)
        {
            if (Token != null)
            {
                Token.Cancel();
            }
            if (Stream != null)
            {
                Stream.Close();
                Stream.Dispose();
            }
            Stream = null;

            if (deleteFile)
            {
                try
                {
                    if (File.Exists(CacheFileName))
                    {
                        File.Delete(CacheFileName);
                    }
                }
                catch (Exception)
                {
                }
            }

            GC.Collect();
            GC.SuppressFinalize(true);
        }

        public bool Check()
        {
            if (Token.IsCancellationRequested || State != UploadState.Uploading)
            {
                Disponse();
                return false;
            }
            return true;
        }

        public class QueueModel
        {
            public byte[] Data { get; set; }
            public int Length { get; set; }
        }
    }

    public class FileSaveManager
    {
        public ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, FileSaveInfo>> Caches { get; } = new();

        public FileSaveInfo Get(ulong clientId, ulong md5)
        {
            Caches.TryGetValue(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                ipDic.TryGetValue(md5, out FileSaveInfo fs);
                return fs;
            }
            return null;
        }

        public void Add(FileSaveInfo info)
        {
            Caches.TryGetValue(info.ClientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic);
            if (ipDic == null)
            {
                ipDic = new ConcurrentDictionary<ulong, FileSaveInfo>();
                Caches.TryAdd(info.ClientId, ipDic);
            }
            info.Token = new CancellationTokenSource();
            ipDic.AddOrUpdate(info.Md5, info, (a, b) => info);
        }

        public bool Contains(ulong clientId, string fileFullName)
        {
            if (Caches.TryGetValue(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic))
            {
                return ipDic.Values.FirstOrDefault(c => c.FileName == fileFullName) != null;
            }
            return false;
        }

        public void SetState(ulong clientId, ulong md5, UploadState state)
        {
            if (Caches.TryGetValue(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic))
            {
                if (ipDic.TryGetValue(md5, out FileSaveInfo save))
                {
                    save.State = state;
                }
            }
        }

        public void Remove(ulong clientId, ulong md5, bool deleteFile = false)
        {
            if (Caches.TryGetValue(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic))
            {
                if (ipDic.TryRemove(md5, out FileSaveInfo save))
                {
                    save.Disponse(deleteFile);
                    save.Token.Cancel();
                }
            }
        }

        public void Clear(ulong clientId)
        {
            if (Caches.TryRemove(clientId, out ConcurrentDictionary<ulong, FileSaveInfo> ipDic))
            {
                foreach (var item in ipDic.Values)
                {
                    item.Disponse();
                }
                ipDic.Clear();
            }
        }

    }

    public class FtpPluginParamWrap : PluginParamWrap
    {
        public ClientInfo Client { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }
    }

    [ProtoContract, MessagePackObject]
    public class FtpResultModel
    {
        [ProtoMember(1, IsRequired = true), Key(1)]
        public FtpResultCodes Code { get; set; } = FtpResultCodes.OK;

        /// <summary>
        /// 写数据
        /// </summary>
        [ProtoMember(2, IsRequired = true), Key(2)]
        public object Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 读数据
        /// </summary>
        [ProtoIgnore, IgnoreMember]
        public ReadOnlyMemory<byte> ReadData { get; set; }


        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        [Flags]
        public enum FtpResultCodes : byte
        {
            [Description("成功")]
            OK = 0,
            [Description("禁用了")]
            DISABLE = 1,
            [Description("目录不可空")]
            PATH_REQUIRED = 2,
            [Description("出错了")]
            UNKNOW = 255
        }

        public byte[] ToBytes()
        {
            byte[] dataBytes = Data is byte[]? Data as byte[] : Data.ToBytes();

            byte[] result = new byte[1 + dataBytes.Length];
            result[0] = (byte)Code;
            Array.Copy(dataBytes, 0, result, 1, dataBytes.Length);

            return result;
        }

        public static FtpResultModel FromBytes(ReadOnlyMemory<byte> bytes)
        {
            return new FtpResultModel
            {
                Code = (FtpResultCodes)bytes.Span[0],
                ReadData = bytes.Slice(1, bytes.Length - 1)
            };
        }
    }
}
