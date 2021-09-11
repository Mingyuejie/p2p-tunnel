using common.extends;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public class FileServerDownloadPlugin : IFileServerPlugin
    {
        private readonly FileServerEventHandles fileServerEventHandles;
        public FileServerDownloadPlugin(FileServerEventHandles fileServerEventHandles)
        {
            this.fileServerEventHandles = fileServerEventHandles;
        }

        public FileServerCmdTypes Type => FileServerCmdTypes.DOWNLOAD;

        public void Excute(TcpFileMessageEventArg arg)
        {
            fileServerEventHandles.OnTcpDownload(new  TcpEventArg<FileServerDownloadModel>  
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<FileServerDownloadModel>()
            });
        }
    }

    public class FileServerFilePlugin : IFileServerPlugin
    {
        private readonly FileServerEventHandles fileServerEventHandles;
        public FileServerFilePlugin(FileServerEventHandles fileServerEventHandles)
        {
            this.fileServerEventHandles = fileServerEventHandles;
        }

        public FileServerCmdTypes Type => FileServerCmdTypes.FILE;

        public void Excute(TcpFileMessageEventArg arg)
        {
            fileServerEventHandles.OnTcpFile(new  TcpEventArg<FileModel>
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<FileModel>()
            });
        }
    }

    public class FileServerProgressPlugin : IFileServerPlugin
    {
        private readonly FileServerEventHandles fileServerEventHandles;
        public FileServerProgressPlugin(FileServerEventHandles fileServerEventHandles)
        {
            this.fileServerEventHandles = fileServerEventHandles;
        }

        public FileServerCmdTypes Type => FileServerCmdTypes.PROGRESS;

        public void Excute(TcpFileMessageEventArg arg)
        {
            fileServerEventHandles.OnTcpProgress(new  TcpEventArg<FileServerProgressModel>
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<FileServerProgressModel>()
            });
        }
    }
}
