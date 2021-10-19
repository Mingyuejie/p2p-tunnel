using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace client.service.ftp.server.plugin
{
    public class ListPlugin : IFtpServerPlugin
    {
        private readonly FtpServer ftpServer;

        public ListPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.LIST;

        public object Excute(FtpPluginParamWrap data)
        {
            FtpListCommand cmd = data.Data.DeBytes<FtpListCommand>();
            return ftpServer.GetFiles(cmd);
        }
    }
}