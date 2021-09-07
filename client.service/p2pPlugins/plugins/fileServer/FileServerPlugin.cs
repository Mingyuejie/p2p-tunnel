using common.extends;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public class FileServerPlugin : IP2PPlugin
    {
        public P2PDataTypes Type => P2PDataTypes.FILE_SERVER;

        public void Excute(OnP2PTcpArg arg)
        {
            FileServerEventHandles.Instance.OnTcpFileServer(new TcpFileMessageEventArg
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
}
