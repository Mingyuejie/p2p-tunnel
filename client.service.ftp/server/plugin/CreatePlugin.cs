﻿using client.service.ftp.extends;
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
    public class CreatePlugin : IFtpServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly Config config;
        public CreatePlugin(Config config)
        {
            this.config = config;
        }

        public object Excute(PluginParamWrap arg)
        {
            FtpCreateCommand cmd = arg.Wrap.Content.DeBytes<FtpCreateCommand>();

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                arg.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "目录不可为空");
            }
            else
            {
                List<string> errs = cmd.Path.CreateDir(config.ServerCurrentPath, config.ServerRoot);
                if (errs.Any())
                {
                    arg.SetCode(ServerMessageResponeCodes.ACCESS, string.Join(",", errs));
                }
            }

            return true;
        }
    }
}
