﻿using client.messengers.clients;
using client.messengers.punchHole.tcp;
using client.messengers.register;
using client.service.ftp.commands;
using common;
using Microsoft.Extensions.DependencyInjection;
using server;
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
        protected override string SocketPath => "ftpclient/Execute";
        protected override string RootPath { get { return config.ServerRoot; } }

        public FtpServer(ServiceProvider serviceProvider, MessengerSender messengerSender, Config config, IClientInfoCaching clientInfoCaching, RegisterStateInfo registerState, IPunchHoleTcp punchHoleTcp, IDataTunnelRegister dataTunnelRegister)
            : base(serviceProvider, messengerSender, config, clientInfoCaching, registerState, punchHoleTcp, dataTunnelRegister)
        {
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                currentPaths.TryRemove(client.Id, out _);
            });
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            LoadPlugins(assemblys, typeof(IFtpCommandServerPlugin));
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
        public new async ValueTask OnFile(FtpFileCommand cmd, FtpPluginParamWrap wrap)
        {
            await base.OnFile(cmd, wrap);
        }
        public async Task<IEnumerable<string>> Upload(FtpDownloadCommand cmd, FtpPluginParamWrap wrap)
        {
            string[] paths = cmd.Path.Split(Helper.SeparatorChar);
            string currentPath = GetCurrentPath(wrap.Client.Id);
            IEnumerable<string> accessPaths = paths.Where(c => Path.Combine(currentPath, c).StartsWith(config.ServerRoot));
            IEnumerable<string> notAccessPaths = paths.Except(accessPaths);
            if (accessPaths.Any())
            {
                await Upload(currentPath, string.Join(Helper.SeparatorChar, accessPaths), wrap.Client);
            }
            if (notAccessPaths.Any())
            {
                return notAccessPaths.Select(c => c + " 无目录权限");
            }

            return Array.Empty<string>();
        }

        private ConcurrentDictionary<ulong, string> currentPaths = new ConcurrentDictionary<ulong, string>();
        public string GetCurrentPath(ulong clientId)
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

}
