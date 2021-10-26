﻿using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.service.ftp.extends;
using client.service.ftp.protocol;
using common;
using common.extends;
using MessagePack;
using ProtoBuf;
using server.model;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private long fileId = 0;

        protected readonly IServerRequest serverRequest;
        protected readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        public FtpBase(IServerRequest serverRequest, Config config, IClientInfoCaching clientInfoCaching)
        {
            this.serverRequest = serverRequest;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            clientInfoCaching.OnTcpOffline.Sub((client) =>
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
        protected void OnFile(string currentPath, FtpFileCommand cmd, ClientInfo client)
        {
            try
            {
                var fs = Downloads.Get(cmd.SessionId, cmd.Md5);
                if (fs != null && fs.Token.IsCancellationRequested)
                {
                    return;
                }
                if (fs == null)
                {
                    fs = new FileSaveInfo
                    {
                        Stream = null,
                        IndexLength = 0,
                        TotalLength = cmd.Size,
                        FileName = Path.Combine(currentPath, cmd.Name),
                        CacheFileName = Path.Combine(currentPath, $"{cmd.Md5}.downloading"),
                        ClientId = client.Id,
                        Md5 = cmd.Md5,
                        State = UploadState.Wait
                    };
                    Downloads.Add(fs);
                }
                if (fs.State == UploadState.Canceled)
                {
                    return;
                }
                if (cmd.Data == null || cmd.Data.Length == 0)
                {
                    return;
                }

                if (fs.Stream == null)
                {
                    fs.CacheFileName.TryDeleteFile();
                    fs.Stream = new FileStream(fs.CacheFileName, FileMode.Create & FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    fs.Stream.Seek(cmd.Size - 1, SeekOrigin.Begin);
                    fs.Stream.WriteByte(new byte());
                    fs.Stream.Seek(0, SeekOrigin.Begin);
                }

                fs.Stream.Write(cmd.Data);
                fs.IndexLength += cmd.Data.Length;

                if (fs.IndexLength >= cmd.Size)
                {
                    SendOnlyTcp(new FtpFileEndCommand { SessionId = client.SelfId, Md5 = cmd.Md5 }, client.Id);
                    Downloads.Remove(cmd.SessionId, cmd.Md5);
                    File.Move(fs.CacheFileName, fs.FileName, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"{ex}");
            }
        }

        public void OnFileEnd(FtpFileEndCommand cmd)
        {
            Uploads.Remove(cmd.SessionId, cmd.Md5);
        }
        public void OnFileUploadCancel(FtpCancelCommand cmd)
        {
            Uploads.Remove(cmd.SessionId, cmd.Md5);
            clientInfoCaching.Get(cmd.SessionId, out ClientInfo client);
            if (client != null)
            {
                SendOnlyTcp(new FtpCanceledCommand { SessionId = client.SelfId, Md5 = cmd.Md5 }, client.Id);
            };
        }
        public void OnFileUploadCanceled(FtpCanceledCommand cmd)
        {
            Downloads.Remove(cmd.SessionId, cmd.Md5, true);
        }
        protected void Upload(string currentPath, string path, ClientInfo client)
        {
            Task.Run(() =>
            {
                foreach (var item in path.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        var filepath = Path.Combine(currentPath, item);
                        if (Directory.Exists(filepath))
                        {
                            List<FileUploadInfo> files = new();
                            GetFiles(files, new DirectoryInfo(filepath));

                            var paths = files.Where(c => c.Type == FileType.Folder)
                                .Select(c => c.Path.Replace(currentPath, "").TrimStart(Path.DirectorySeparatorChar));
                            if (paths.Any())
                            {
                                RemoteCreate(string.Join(",", paths), client).Wait();
                            }
                            foreach (var file in files.Where(c => c.Type == FileType.File))
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

                Interlocked.Increment(ref fileId);
                var save = new FileSaveInfo
                {
                    FileName = file.FullName,
                    IndexLength = 0,
                    TotalLength = file.Length,
                    Md5 = fileId,
                    ClientId = client.Id,
                    State = UploadState.Wait,
                    CacheFileName = file.FullName.Replace(currentPath, "").TrimStart(Path.DirectorySeparatorChar)
                };
                Uploads.Add(save);

                SendOnlyTcp(new FtpFileCommand
                {
                    SessionId = client.SelfId,
                    Md5 = save.Md5,
                    Size = save.TotalLength,
                    Name = save.CacheFileName,
                    Data = Array.Empty<byte>()
                }, client.Id);
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
            if (client == null) return;

            FtpFileCommand cmd = new FtpFileCommand
            {
                SessionId = client.SelfId,
                Md5 = save.Md5,
                Size = save.TotalLength,
                Name = save.CacheFileName
            };
            save.State = UploadState.Uploading;
            int packSize = 32 * 1024; //每个包大小
            int packCount = (int)(save.TotalLength / packSize);
            int lastPackSize = (int)(save.TotalLength - (packCount * packSize));

            Task.Run(() =>
            {
                try
                {
                    using FileStream fs = new FileStream(save.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    int index = 0;
                    while (index < packCount)
                    {
                        if (save.DataQueue.Count < 2000)
                        {
                            if (!save.Check())
                            {
                                return;
                            }

                            byte[] data = new byte[packSize];
                            fs.Read(data, 0, packSize);
                            cmd.Data = data;

                            save.DataQueue.Enqueue(new FileSaveInfo.QueueModel
                            {
                                Data = cmd.ToBytes(),
                                Length = data.Length
                            });

                            index++;
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
                    if (!save.Check())
                    {
                        return;
                    }
                    if (lastPackSize > 0)
                    {
                        byte[] data = new byte[lastPackSize];
                        fs.Read(data, 0, lastPackSize);
                        cmd.Data = data;
                        save.DataQueue.Enqueue(new FileSaveInfo.QueueModel
                        {
                            Data = cmd.ToBytes(),
                            Length = data.Length
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex);
                    save.Disponse();
                }
            }, save.Token.Token);

            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        if (!save.Check())
                        {
                            break;
                        }

                        if (!save.DataQueue.IsEmpty)
                        {
                            save.DataQueue.TryDequeue(out FileSaveInfo.QueueModel model);

                            if (!SendOnlyTcp(model.Data, client.Socket, cmd.Cmd, cmd.SessionId))
                            {
                                save.State = UploadState.Error;
                            }
                            save.IndexLength += model.Length;
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    save.Disponse();
                    save.State = UploadState.Error;
                    Logger.Instance.Error($" Upload {ex}");
                }
            }, save.Token.Token);
        }

        public async Task<CommonTaskResponseModel<bool>> RemoteCreate(string path, ClientInfo client)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpCreateCommand { SessionId = client.SelfId, Path = path }, client.Id);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }
        public async Task<CommonTaskResponseModel<bool>> RemoteCancel(long md5, ClientInfo client)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpCancelCommand { SessionId = client.SelfId, Md5 = md5 }, client.Id);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }
        public async Task<CommonTaskResponseModel<bool>> RemoteDelete(string path, ClientInfo client)
        {
            CommonTaskResponseModel<bool> res = new CommonTaskResponseModel<bool> { Data = true };
            var response = await SendReplyTcp(new FtpDelCommand { SessionId = client.SelfId, Path = path }, client.Id);
            if (response.Code != ServerMessageResponeCodes.OK)
            {
                res.ErrorMsg = response.ErrorMsg;
                res.Data = false;
            }
            return res;
        }

        protected bool SendOnlyTcp<T>(T data, long clientId)
        {
            clientInfoCaching.Get(clientId, out ClientInfo client);
            if (client != null)
            {
                return SendOnlyTcp(data, client.Socket);
            }
            return false;
        }
        protected bool SendOnlyTcp<T>(T data, Socket socket)
        {
            if (socket != null)
            {
                IFtpCommandBase _base = (IFtpCommandBase)data;
                bool res = SendOnlyTcp(data.ToBytes(), socket, _base.Cmd, _base.SessionId);
                return res;
            }
            return false;
        }
        protected bool SendOnlyTcp(byte[] data, Socket socket, FtpCommand cmd, long sessionid)
        {
            if (socket != null)
            {
                var res = serverRequest.SendOnlyTcp(new SendTcpEventArg<byte[]>
                {
                    Data = AddAttributes(data, cmd, sessionid),
                    Path = SocketPath,
                    Socket = socket
                });
               
                return res;
            }
            return false;
        }
        protected async Task<ServerMessageResponeWrap> SendReplyTcp<T>(T data, long clientId)
        {
            clientInfoCaching.Get(clientId, out ClientInfo client);

            IFtpCommandBase _base = (IFtpCommandBase)data;
            return await serverRequest.SendReplyTcp(new SendTcpEventArg<byte[]>
            {
                Data = AddAttributes(data.ToBytes(), _base.Cmd, _base.SessionId),
                Path = SocketPath,
                Socket = client?.Socket ?? null
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
            Task.Factory.StartNew(() =>
            {
                int interval = 1000;
                long us = 0;
                while (true)
                {
                    var watch = new MyStopwatch();
                    watch.Start();

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

                        var saves = Uploads.Caches.SelectMany(c => c.Value.Values);
                        if (saves.Count(c => c.State == UploadState.Uploading) < config.UploadNum)
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

                    watch.Stop();
                    us += watch.GetUs();
                    if (us > 1000)
                    {
                        int ms = (int)(us / 1000);
                        us = us - ms * 1000;
                        Helper.Sleep(interval - ms);
                    }
                    else
                    {
                        Helper.Sleep(interval);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        protected byte[] AddAttributes(byte[] bytes, FtpCommand cmd, long sessionid)
        {
            byte cmdByte = (byte)cmd;

            byte[] sessionidBytes = BitConverter.GetBytes(sessionid);

            var res = new byte[1 + sessionidBytes.Length + bytes.Length];
            res[0] = cmdByte;
            int index = 1;

            Array.Copy(sessionidBytes, 0, res, index, sessionidBytes.Length);
            index += sessionidBytes.Length;

            Array.Copy(bytes, 0, res, index, bytes.Length);
            index += bytes.Length;

            return res;
        }
        public FtpCommandBase ReadAttribute(ReadOnlyMemory<byte> bytes)
        {
            FtpCommandBase cmdBase = new FtpCommandBase();
            cmdBase.Cmd = (FtpCommand)bytes.Span[0];
            int index = 1;

            cmdBase.SessionId = BitConverter.ToInt64(bytes.Span.Slice(index, 8));
            index += 8;

            cmdBase.Data = bytes.Slice(index, bytes.Length - index);

            return cmdBase;
        }
        public Memory<byte> RemoveAttribute(byte[] bytes)
        {
            return bytes.AsMemory().Slice(1 + 8);
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FileType : byte
    {
        Folder = 0, File = 1
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
        Wait = 0, Uploading = 1, Canceled = 2, Error = 3
    }

    public class FileSaveInfo
    {
        [JsonIgnore]
        public FileStream Stream { get; set; }
        [JsonIgnore]
        public long ClientId { get; set; }
        public long Md5 { get; set; }
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

        [JsonIgnore]
        public ConcurrentQueue<QueueModel> DataQueue = new ConcurrentQueue<QueueModel>();

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
            DataQueue.Clear();

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
        public ConcurrentDictionary<long, ConcurrentDictionary<long, FileSaveInfo>> Caches { get; } = new();

        public FileSaveInfo Get(long clientId, long md5)
        {
            Caches.TryGetValue(clientId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                ipDic.TryGetValue(md5, out FileSaveInfo fs);
                return fs;
            }
            return null;
        }

        public void Add(FileSaveInfo info)
        {
            Caches.TryGetValue(info.ClientId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic == null)
            {
                ipDic = new ConcurrentDictionary<long, FileSaveInfo>();
                Caches.TryAdd(info.ClientId, ipDic);
            }
            info.Token = new CancellationTokenSource();
            ipDic.AddOrUpdate(info.Md5, info, (a, b) => info);
        }

        public bool Contains(long clientId, string fileFullName)
        {
            Caches.TryGetValue(clientId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                return ipDic.Values.FirstOrDefault(c => c.FileName == fileFullName) != null;
            }
            return false;
        }

        public void SetState(long clientId, long md5, UploadState state)
        {
            Caches.TryGetValue(clientId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                ipDic.TryGetValue(md5, out FileSaveInfo save);
                if (save != null)
                    save.State = state;
            }
        }

        public void Remove(long clientId, long md5, bool deleteFile = false)
        {
            Caches.TryGetValue(clientId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
            {
                ipDic.TryRemove(md5, out FileSaveInfo save);
                if (save != null)
                {
                    save.Disponse(deleteFile);
                }
            }
        }

        public void Clear(long clientId)
        {
            Caches.TryRemove(clientId, out ConcurrentDictionary<long, FileSaveInfo> ipDic);
            if (ipDic != null)
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
}
