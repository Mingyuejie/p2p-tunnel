﻿using client.service.ftp.plugin;
using client.service.ftp.protocol;
using common.extends;
using ProtoBuf;
using server.model;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace client.service.ftp.client.plugin
{
    public class FilePlugin : IFtpClientPlugin
    {
        private readonly FtpClient ftpClient;
        public FilePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public object Excute(FtpPluginParamWrap data)
        {
            FtpFileCommand cmd = data.Wrap.Content.DeBytes<FtpFileCommand>();
            ftpClient.OnFile(cmd, data);
            return null;
        }
    }

    public class FileProgressPlugin : IFtpClientPlugin
    {
        private readonly FtpClient ftpClient;
        public FileProgressPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE_PROGRESS;

        public object Excute(FtpPluginParamWrap data)
        {
            FtpFileProgressCommand cmd = data.Wrap.Content.DeBytes<FtpFileProgressCommand>();
            ftpClient.OnUploadFileProgress(cmd);
            return null;
        }
    }

    public class FileEndPlugin : IFtpClientPlugin
    {
        private readonly FtpClient ftpClient;
        public FileEndPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE_END;

        public object Excute(FtpPluginParamWrap data)
        {
            FtpFileEndCommand cmd = data.Wrap.Content.DeBytes<FtpFileEndCommand>();
            ftpClient.OnFileEnd(cmd);
            return null;
        }
    }
}
