using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace client.service.ftp.server
{
    public class FtpServer : FtpBase
    {
        public Dictionary<FtpCommand, IFtpServerPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpServerPlugin>();

        protected override string SocketPath => "ftpclient/excute";
        protected override string RootPath { get { return config.ServerRoot; } }


        private readonly ServiceProvider serviceProvider;

        public FtpServer(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config, IClientInfoCaching clientInfoCaching)
            : base(serverRequest, config, clientInfoCaching)
        {
            this.serviceProvider = serviceProvider;

            clientInfoCaching.OnTcpOffline.Sub((client) =>
            {
                currentPaths.TryRemove(client.Id, out _);
            });
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            var types = assemblys
               .SelectMany(c => c.GetTypes())
               .Where(c => c.GetInterfaces().Contains(typeof(IFtpServerPlugin)));
            foreach (var item in types)
            {
                IFtpServerPlugin obj = (IFtpServerPlugin)serviceProvider.GetService(item);
                if (!Plugins.ContainsKey(obj.Cmd))
                {
                    Plugins.TryAdd(obj.Cmd, obj);
                }
            }
        }

        public FileInfo[] GetFiles(FtpListCommand cmd)
        {
            DirectoryInfo dirInfo = JoinPath(cmd);
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

        public List<string> Create(FtpCreateCommand cmd)
        {
            return Create(GetCurrentPath(cmd.SessionId), cmd.Path);
        }
        public List<string> Delete(FtpDelCommand cmd)
        {
            return Delete(GetCurrentPath(cmd.SessionId), cmd.Path);
        }
        public void OnFile(FtpFileCommand cmd, FtpPluginParamWrap wrap)
        {
            OnFile(GetCurrentPath(cmd.SessionId), cmd, wrap.Client);
        }
        public IEnumerable<string> Upload(FtpDownloadCommand cmd, FtpPluginParamWrap wrap)
        {
            string[] paths = cmd.Path.Split(',');
            string currentPath = GetCurrentPath(cmd.SessionId);
            IEnumerable<string> accessPaths = paths.Where(c => Path.Combine(currentPath, c).StartsWith(config.ServerRoot));
            IEnumerable<string> notAccessPaths = paths.Except(accessPaths);
            if (accessPaths.Any())
            {
                Upload(currentPath, string.Join(',', accessPaths), wrap.Client);
            }
            if (notAccessPaths.Any())
            {
                return notAccessPaths.Select(c => c + " 无目录权限");
            }

            return Array.Empty<string>();
        }

        private ConcurrentDictionary<long, string> currentPaths = new ConcurrentDictionary<long, string>();
        private string GetCurrentPath(long sessionId)
        {
            currentPaths.TryGetValue(sessionId, out string path);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = config.ServerRoot;
                SetCurrentPath(path, sessionId);
            }
            return path;
        }
        private void SetCurrentPath(string path, long sessionId)
        {
            currentPaths.AddOrUpdate(sessionId, path, (a, b) => path);
        }
        private DirectoryInfo JoinPath(FtpListCommand cmd)
        {
            string currentPath = GetCurrentPath(cmd.SessionId);

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
            SetCurrentPath(dirInfo.FullName, cmd.SessionId);

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

        public object Excute(PluginParamWrap data)
        {
            if (!config.Enable)
            {
                data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "服务未启用");
            }
            else
            {
                FtpCommandBase cmd = ftpServer.ReadAttribute(data.Wrap.Content);
                //data.Wrap.Content = Array.Empty<byte>();
                FtpPluginParamWrap wrap = new FtpPluginParamWrap
                {
                    Code = data.Code,
                    Packet = data.Packet,
                    ServerType = data.ServerType,
                    SourcePoint = data.SourcePoint,
                    TcpSocket = data.TcpSocket,
                    Wrap = data.Wrap,
                    Data = cmd.Data
                };
                wrap.SetErrorMessage(data.ErrorMessage);
                if (clientInfoCaching.Get(cmd.SessionId, out ClientInfo client))
                {
                    wrap.Client = client;
                }
                if (wrap.Client == null)
                {
                    data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, $"未找到来源客户端信息");
                }
                else
                {
                    if (ftpServer.Plugins.ContainsKey(cmd.Cmd))
                    {
                        try
                        {
                            return ftpServer.Plugins[cmd.Cmd].Excute(wrap);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error(ex + "");
                            data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, ex.Message);
                        }
                    }
                }
            }

            return null;
        }
    }
}
