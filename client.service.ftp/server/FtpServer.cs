using client.plugins.serverPlugins;
using client.service.ftp.plugin;
using client.service.ftp.protocol;
using client.service.ftp.server.plugin;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
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
    public class FtpServer
    {
        public Dictionary<FtpCommand, IFtpPlugin> Plugins { get; } = new Dictionary<FtpCommand, IFtpPlugin>();
        private readonly ConcurrentDictionary<string, FileSaveInfo> files = new();
        private readonly ConcurrentDictionary<string, string> sends = new();

        private readonly ServiceProvider serviceProvider;
        private readonly IServerRequest serverRequest;
        private readonly Config config;
        public FtpServer(ServiceProvider serviceProvider, IServerRequest serverRequest, Config config)
        {
            this.serviceProvider = serviceProvider;
            this.serverRequest = serverRequest;
            this.config = config;
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            var types = assemblys
               .SelectMany(c => c.GetTypes())
               .Where(c => c.GetInterfaces().Contains(typeof(IFtpPlugin)));
            foreach (var item in types)
            {
                IFtpPlugin obj = (IFtpPlugin)serviceProvider.GetService(item);
                if (!Plugins.ContainsKey(obj.Cmd))
                {
                    Plugins.TryAdd(obj.Cmd, obj);
                }
            }
        }

        public void OnFile(FtpFileCommand cmd)
        {
            string fullPath = Path.Combine(config.ServerCurrentPath, cmd.Name);
            files.TryGetValue(cmd.Md5, out FileSaveInfo fs);
            if (fs == null)
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                fs = new FileSaveInfo
                {
                    Stream = new FileStream(fullPath, FileMode.Create & FileMode.Append, FileAccess.Write),
                    IndexLength = 0,
                    TotalLength = cmd.Size,
                    FileName = fullPath,
                };
                files.TryAdd(cmd.Md5, fs);
            }
            fs.Stream.Write(cmd.Data);
            fs.IndexLength += cmd.Data.Length;

            if (fs.Stream.Length >= cmd.Size)
            {
                files.TryRemove(cmd.Md5, out _);
                fs.Stream.Close();
            }
        }
        public void SendFile(System.IO.FileInfo file, Socket socket)
        {
            Task.Run(() =>
            {
                try
                {
                    FtpFileCommand cmd = new FtpFileCommand
                    {
                        Md5 = file.FullName.Md5(),
                        Size = file.Length,
                        Name = file.Name
                    };
                    sends.TryAdd(cmd.Md5, file.FullName);

                    int packSize = 1024; //每个包大小 

                    int packCount = (int)(file.Length / packSize);
                    long lastPackSize = file.Length - packCount * packSize;

                    using FileStream fs = file.OpenRead();
                    for (int index = 0; index < packCount; index++)
                    {
                        byte[] data = new byte[packSize];
                        fs.Read(data, 0, packSize);
                        cmd.Data = data;
                        SendOnlyTcp(cmd, socket);
                    }
                    if (lastPackSize > 0)
                    {
                        byte[] data = new byte[lastPackSize];
                        fs.Read(data, 0, (int)lastPackSize);
                        cmd.Data = data;
                        SendOnlyTcp(cmd, socket);
                    }
                }
                catch (Exception)
                {
                }
            });
        }
        public void OnSendFileEnd(FtpFileEndCommand cmd)
        {
            sends.TryRemove(cmd.Md5, out _);
        }

        public void SendOnlyTcp<IFtpCommandBase>(IFtpCommandBase data, Socket socket)
        {
            serverRequest.SendOnlyTcp(new SendTcpEventArg<IFtpCommandBase>
            {
                Data = data,
                Path = "ftpclient/excute",
                Socket = socket
            });
        }
    }

    public class FileSaveInfo
    {
        public FileStream Stream { get; set; }
        public long TotalLength { get; set; }
        public long IndexLength { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class FtpServerPlugin : IPlugin
    {
        private readonly FtpServer ftpServer;
        public FtpServerPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public object Excute(PluginParamWrap data)
        {
            IFtpCommandBase cmd = data.Wrap.Content.DeBytes<IFtpCommandBase>();
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
            return null;
        }
    }
}
