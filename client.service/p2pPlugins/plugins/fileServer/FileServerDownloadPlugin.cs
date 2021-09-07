using common.extends;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public class FileServerDownloadPlugin : IFileServerPlugin
    {
        public FileServerCmdTypes Type => FileServerCmdTypes.DOWNLOAD;

        public void Excute(TcpFileMessageEventArg arg)
        {
            FileServerEventHandles.Instance.OnTcpDownload(new  TcpEventArg<FileServerDownloadModel>  
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<FileServerDownloadModel>()
            });
        }
    }

    public class FileServerFilePlugin : IFileServerPlugin
    {
        public FileServerCmdTypes Type => FileServerCmdTypes.FILE;

        public void Excute(TcpFileMessageEventArg arg)
        {
            FileServerEventHandles.Instance.OnTcpFile(new  TcpEventArg<FileModel>
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<FileModel>()
            });
        }
    }

    public class FileServerProgressPlugin : IFileServerPlugin
    {
        public FileServerCmdTypes Type => FileServerCmdTypes.PROGRESS;

        public void Excute(TcpFileMessageEventArg arg)
        {
            FileServerEventHandles.Instance.OnTcpProgress(new  TcpEventArg<FileServerProgressModel>
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<FileServerProgressModel>()
            });
        }
    }
}
