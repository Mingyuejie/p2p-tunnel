using common.extends;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

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
            obj.AddFileServerPlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.AddSingleton<FileServerHelper>();
            obj.AddSingleton<FileServerEventHandles>();

            return obj;
        }

        public static ServiceCollection AddFileServerPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IFileServerPlugin)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }
            return obj;
        }

        public static ServiceProvider UseFileServerPlugin(this ServiceProvider obj)
        {
            obj.UseFileServerPlugin(AppDomain.CurrentDomain.GetAssemblies());
            return obj;
        }

        public static ServiceProvider UseFileServerPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            obj.GetService<FileServerEventHandles>().LoadPlugins(assemblys);
            return obj;
        }
    }
}
