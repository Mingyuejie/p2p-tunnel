using client.plugins.serverPlugins;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using client.service.ftp.server.plugin;
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
using System.Threading.Tasks;

namespace client.service.ftp.server
{
    public class FtpServer : FtpBase
    {
        public Dictionary<FtpCommand, IFtpServerPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpServerPlugin>();

        protected override string SocketPath => "ftpclient/excute";
        protected override string RootPath { get { return config.ServerRoot; } }

        private readonly ServiceProvider serviceProvider;
        private readonly Config config;

        public FtpServer(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config, IClientInfoCaching clientInfoCaching)
            : base(serverRequest, clientInfoCaching)
        {
            this.serviceProvider = serviceProvider;
            this.config = config;

            clientInfoCaching.OnTcpOffline.Sub((client) =>
            {
                currentPaths.TryRemove(client.IpAddress,out _);
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

        public IEnumerable<FileInfo> GetFiles(string path, PluginParamWrap data)
        {
            DirectoryInfo dirInfo = JoinPath(path, data);
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
            }));
        }

        public List<string> Create(string path, PluginParamWrap data)
        {
            return Create(GetCurrentPath(data), path);
        }
        public List<string> Delete(string path, PluginParamWrap data)
        {
            return Delete(GetCurrentPath(data), path);
        }
        public void OnFile(FtpFileCommand cmd, PluginParamWrap data)
        {
            OnFile(GetCurrentPath(data), cmd, data);
        }
        public void Upload(string path, PluginParamWrap data)
        {
            Upload(GetCurrentPath(data), path, data.TcpSocket);
        }

        private ConcurrentDictionary<long, string> currentPaths = new ConcurrentDictionary<long, string>();
        private string GetCurrentPath(PluginParamWrap data)
        {
            currentPaths.TryGetValue(data.SourcePoint.ToInt64(), out string path);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = config.ClientRootPath;
                SetCurrentPath(path, data);
            }
            return path;
        }
        private void SetCurrentPath(string path, PluginParamWrap data)
        {
            currentPaths.AddOrUpdate(data.SourcePoint.ToInt64(), path, (a, b) => path);
        }
        private DirectoryInfo JoinPath(string path, PluginParamWrap data)
        {
            string currentPath = GetCurrentPath(data);

            DirectoryInfo dirInfo = new DirectoryInfo(currentPath);
            if (!string.IsNullOrWhiteSpace(path))
            {
                dirInfo = new DirectoryInfo(Path.Combine(currentPath, path));
            }
            //不能访问根目录的上级目录
            if (dirInfo.FullName.Length < RootPath.Length)
            {
                dirInfo = new DirectoryInfo(RootPath);
            }
            SetCurrentPath(dirInfo.FullName, data);

            return dirInfo;
        }
    }

    public class FtpServerPlugin : IPlugin
    {
        private readonly FtpServer ftpServer;
        private readonly Config config;
        public FtpServerPlugin(FtpServer ftpServer, Config config)
        {
            this.ftpServer = ftpServer;
            this.config = config;
        }

        public object Excute(PluginParamWrap data)
        {
            if (!config.Enable)
            {
                data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "服务未启用");
            }
            else
            {
                FtpCommandBase cmd = data.Wrap.Content.DeBytes<FtpCommandBase>();
                if (ftpServer.Plugins.ContainsKey(cmd.Cmd))
                {
                    try
                    {
                        return ftpServer.Plugins[cmd.Cmd].Excute(data);
                    }
                    catch (Exception ex)
                    {
                        data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, ex.Message);
                    }
                }
            }

            return null;
        }
    }
}
