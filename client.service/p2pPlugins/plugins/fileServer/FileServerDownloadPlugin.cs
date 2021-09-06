using common.extends;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public class FileServerDownloadPlugin : IP2PFilePlugin
    {
        public P2PFileCmdTypes Type => P2PFileCmdTypes.DOWNLOAD;

        public void Excute(TcpFileMessageEventArg arg)
        {
            FileServerEventHandles.Instance.OnTcpFileDownloadMessage(new TcpFileDownloadMessageEventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.ProtobufDeserialize<P2PFileCmdDownloadModel>()
            });
        }
    }

    public class FileServerFilePlugin : IP2PFilePlugin
    {
        public P2PFileCmdTypes Type => P2PFileCmdTypes.FILE;

        public void Excute(TcpFileMessageEventArg arg)
        {
            FileServerEventHandles.Instance.OnTcpFileFileMessage(new TcpFileFileMessageEventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.ProtobufDeserialize<P2PFileFileModel>()
            });
        }
    }

    public class FileServerProgressPlugin : IP2PFilePlugin
    {
        public P2PFileCmdTypes Type => P2PFileCmdTypes.PROGRESS;

        public void Excute(TcpFileMessageEventArg arg)
        {
            FileServerEventHandles.Instance.OnTcpFileProgressMessage(new TcpFileProgressMessageEventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.ProtobufDeserialize<P2PFileProgressModel>()
            });
        }
    }
}
