using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
using System;
using System.Linq;
using System.Reflection;

namespace client.service.p2pPlugins.fileServer
{
    public class FileServerPlugin : IPlugin
    {
        private readonly FileServerEventHandles fileServerEventHandles;
        private readonly FileServerHelper fileServerHelper;


        public FileServerPlugin(FileServerEventHandles fileServerEventHandles, FileServerHelper fileServerHelper)
        {
            this.fileServerEventHandles = fileServerEventHandles;
            this.fileServerHelper = fileServerHelper;
        }

        public FileInfo[] List(PluginExcuteModel arg)
        {
            FileServerModel model = arg.Wrap.Content.DeBytes<FileServerModel>();
            return fileServerHelper.GetRemoteFiles(model.Data.DeBytes<FileServerListModel>().Path);
        }


        public void Download(PluginExcuteModel arg)
        {
            FileServerModel model = arg.Wrap.Content.DeBytes<FileServerModel>();
            fileServerEventHandles.OnTcpDownload(new TcpEventArg<FileServerDownloadModel>
            {
                Packet = arg,
                RawData = model,
                Data = model.Data.DeBytes<FileServerDownloadModel>()
            });
        }

        public void File(PluginExcuteModel arg)
        {
            FileServerModel model = arg.Wrap.Content.DeBytes<FileServerModel>();
            fileServerEventHandles.OnTcpFile(new TcpEventArg<FileModel>
            {
                Packet = arg,
                RawData = model,
                Data = model.Data.DeBytes<FileModel>()
            });
        }

        public void Progress(PluginExcuteModel arg)
        {
            FileServerModel model = arg.Wrap.Content.DeBytes<FileServerModel>();
            fileServerEventHandles.OnTcpProgress(new TcpEventArg<FileServerProgressModel>
            {
                Packet = arg,
                RawData = model,
                Data = model.Data.DeBytes<FileServerProgressModel>()
            });
        }
    }


    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddFileServerPlugin(this ServiceCollection obj)
        {
            obj.AddFileServerPlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.AddSingleton<FileServerHelper>();
            obj.AddSingleton<FileServerEventHandles>();

            return obj;
        }

        public static ServiceCollection AddFileServerPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            return obj;
        }

        public static ServiceProvider UseFileServerPlugin(this ServiceProvider obj)
        {
            obj.UseFileServerPlugin(AppDomain.CurrentDomain.GetAssemblies());
            return obj;
        }

        public static ServiceProvider UseFileServerPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            return obj;
        }
    }
}
