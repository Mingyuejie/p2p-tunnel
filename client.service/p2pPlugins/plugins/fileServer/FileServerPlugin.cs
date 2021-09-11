using common.extends;
using Microsoft.Extensions.DependencyInjection;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public class FileServerPlugin : IP2PPlugin
    {
        private readonly FileServerEventHandles fileServerEventHandles;
        public FileServerPlugin(FileServerEventHandles fileServerEventHandles)
        {
            this.fileServerEventHandles = fileServerEventHandles;
        }

        public P2PDataTypes Type => P2PDataTypes.FILE_SERVER;

        public void Excute(OnP2PTcpArg arg)
        {
            fileServerEventHandles.OnTcpFileServer(new TcpFileMessageEventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<FileServerModel>()
            });
        }
    }

    public interface IFileServerPlugin
    {
        FileServerCmdTypes Type { get; }

        void Excute(TcpFileMessageEventArg arg);
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddFileServerPlugin(this ServiceCollection obj)
        {
            obj.AddSingleton<FileRequestPlugin>();

            obj.AddSingleton<FileServerDownloadPlugin>();
            obj.AddSingleton<FileServerFilePlugin>();
            obj.AddSingleton<FileServerProgressPlugin>();
            obj.AddSingleton<FileServerPlugin>();
            obj.AddSingleton<FileServerHelper>();
            obj.AddSingleton<FileServerEventHandles>();

            return obj;
        }
        public static ServiceProvider UseFileServerPlugin(this ServiceProvider obj)
        {
            obj.GetService<FileServerEventHandles>().LoadPlugins();
            return obj;
        }
    }
}
