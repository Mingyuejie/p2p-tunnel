using client.service.p2pPlugins.plugins.fileServer;
using client.service.p2pPlugins.plugins.request;
using common.extends;

namespace client.service.p2pPlugins.plugins
{
    public class FileRequestPlugin : IRequestExcutePlugin
    {
        private readonly FileServerHelper fileServerHelper;
        public FileRequestPlugin(FileServerHelper fileServerHelper)
        {
            this.fileServerHelper = fileServerHelper;
        }

        public void List(PluginExcuteWrap arg)
        {
            var model = arg.Data.Data.Data.DeBytes<FileServerListModel>();
            arg.Callback(arg, fileServerHelper.GetRemoteFiles(model.Path).ToBytes());
        }
    }
}
