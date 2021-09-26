﻿using client.service.ftp.protocol;
using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ftp.plugin
{
    public interface IFtpPlugin
    {
        public FtpCommand Cmd { get; }
        public object Excute(PluginParamWrap data);
    }
}