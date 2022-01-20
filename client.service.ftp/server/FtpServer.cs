﻿using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server;
using server.model;
using server.plugin;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.ftp.server
{
    public class FtpServer : FtpBase
    {
        public Dictionary<FtpCommand, IFtpServerPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpServerPlugin>();

        protected override string SocketPath => "ftpclient/Execute";
        protected override string RootPath { get { return config.ServerRoot; } }

        private readonly ServiceProvider serviceProvider;

        public FtpServer(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config, IClientInfoCaching clientInfoCaching)
            : base(serverRequest, config, clientInfoCaching)
        {
            this.serviceProvider = serviceProvider;

            clientInfoCaching.OnOffline.Sub((client) =>
            {
                currentPaths.TryRemove(client.Id, out _);
            });
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IFtpServerPlugin)))
            {
                IFtpServerPlugin obj = (IFtpServerPlugin)serviceProvider.GetService(item);
                if (!Plugins.ContainsKey(obj.Cmd))
                {
                    Plugins.TryAdd(obj.Cmd, obj);
                }
            }
        }

        public FileInfo[] GetFiles(FtpListCommand cmd, FtpPluginParamWrap wrap)
        {
            DirectoryInfo dirInfo = JoinPath(cmd, wrap.Client.Id);
            return dirInfo.GetDirectories()
                .Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                .Select(c => new FileInfo
                {
                    CreationTime = c.CreationTime,
                    Length = 0,
                    Name = c.Name,
                    LastWriteTime = c.LastWriteTime,
                    LastAccessTime = c.LastAccessTime,
                    Type = FileType.Folder,
                }).Concat(dirInfo.GetFiles()
            .Where(c => (c.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            .Select(c => new FileInfo
            {
                CreationTime = c.CreationTime,
                Length = c.Length,
                Name = c.Name,
                LastWriteTime = c.LastWriteTime,
                LastAccessTime = c.LastAccessTime,
                Type = FileType.File,
            })).ToArray();
        }
        public List<string> Create(FtpCreateCommand cmd, FtpPluginParamWrap wrap)
        {
            return Create(GetCurrentPath(wrap.Client.Id), cmd.Path);
        }
        public List<string> Delete(FtpDelCommand cmd, FtpPluginParamWrap wrap)
        {
            return Delete(GetCurrentPath(wrap.Client.Id), cmd.Path);
        }
        public async Task OnFile(FtpFileCommand cmd, FtpPluginParamWrap wrap)
        {
            await OnFile(GetCurrentPath(wrap.Client.Id), cmd, wrap);
        }
        public IEnumerable<string> Upload(FtpDownloadCommand cmd, FtpPluginParamWrap wrap)
        {
            string[] paths = cmd.Path.Split(Helper.SeparatorChar);
            string currentPath = GetCurrentPath(wrap.Client.Id);
            IEnumerable<string> accessPaths = paths.Where(c => Path.Combine(currentPath, c).StartsWith(config.ServerRoot));
            IEnumerable<string> notAccessPaths = paths.Except(accessPaths);
            if (accessPaths.Any())
            {
                Upload(currentPath, string.Join(Helper.SeparatorChar, accessPaths), wrap.Client).Wait();
            }
            if (notAccessPaths.Any())
            {
                return notAccessPaths.Select(c => c + " 无目录权限");
            }

            return Array.Empty<string>();
        }

        private ConcurrentDictionary<ulong, string> currentPaths = new ConcurrentDictionary<ulong, string>();
        private string GetCurrentPath(ulong clientId)
        {
            currentPaths.TryGetValue(clientId, out string path);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = config.ServerRoot;
                SetCurrentPath(path, clientId);
            }
            return path;
        }
        private void SetCurrentPath(string path, ulong clientId)
        {
            currentPaths.AddOrUpdate(clientId, path, (a, b) => path);
        }
        private DirectoryInfo JoinPath(FtpListCommand cmd, ulong clientId)
        {
            string currentPath = GetCurrentPath(clientId);

            DirectoryInfo dirInfo = new DirectoryInfo(currentPath);
            if (!string.IsNullOrWhiteSpace(cmd.Path))
            {
                dirInfo = new DirectoryInfo(Path.Combine(currentPath, cmd.Path));
            }
            //不能访问根目录的上级目录
            if (dirInfo.FullName.Length < RootPath.Length)
            {
                dirInfo = new DirectoryInfo(RootPath);
            }
            SetCurrentPath(dirInfo.FullName, clientId);

            return dirInfo;
        }

    }

    public class FtpServerPlugin : IPlugin
    {
        private readonly FtpServer ftpServer;
        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        public FtpServerPlugin(FtpServer ftpServer, Config config, IClientInfoCaching clientInfoCaching)
        {
            this.ftpServer = ftpServer;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
        }

        public async Task<byte[]> Execute(IConnection connection)
        {
            if (!config.Enable)
            {
                return new FtpResultModel
                {
                    Code = FtpResultModel.FtpResultCodes.DISABLE
                }.ToBytes();
            }
            else
            {
                FtpCommand cmd = (FtpCommand)connection.ReceiveRequestWrap.Memory.Span[0];
                FtpPluginParamWrap wrap = new FtpPluginParamWrap
                {
                    Connection = connection,
                };
                if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
                {
                    wrap.Client = client;
                    if (ftpServer.Plugins.ContainsKey(cmd))
                    {
                        try
                        {
                            FtpResultModel res = await ftpServer.Plugins[cmd].Execute(wrap);
                            if (res != null)
                            {
                                return res.ToBytes();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error(ex);
                            return new FtpResultModel
                            {
                                Code = FtpResultModel.FtpResultCodes.UNKNOW
                            }.ToBytes();
                        }
                    }
                }
            }

            return null;
        }
    }


}
