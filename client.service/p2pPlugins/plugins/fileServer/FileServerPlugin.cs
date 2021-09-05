using common.extends;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public class FileServerPlugin : IP2PMessagePlugin
    {
        public P2PDataMessageTypes Type => P2PDataMessageTypes.FILE_SERVER;

        public void Excute(OnP2PTcpMessageArg arg)
        {
            FileServerEventHandles.Instance.OnTcpFileMessage(new TcpFileMessageEventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.ProtobufDeserialize<P2PFileModel>()
            });
        }
    }
}
