﻿using MessagePack;
using ProtoBuf;
using System;
using System.ComponentModel;

namespace client.service.ftp.commands
{
    public enum FtpPluginMode : byte
    {
        SERVER = 0,
        CLIENT = 1,
    }


    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FtpCommand : byte
    {
        [Description("列表")]
        LIST = 0,
        [Description("删除")]
        DELETE = 1,
        [Description("创建")]
        CREATE = 2,
        [Description("下载")]
        DOWNLOAD = 3,
        [Description("传送文件")]
        FILE = 4,
        [Description("传送文件结束")]
        FILE_END = 5,
        [Description("传送文件取消")]
        FILE_CANCEL = 6,
        [Description("传送文件已取消")]
        FILE_CANCELED = 7,
        [Description("传送文件错误")]
        FILE_ERROR = 8,
    }

    [ProtoContract]
    public interface IFtpCommandBase
    {
        public FtpCommand Cmd { get; }

        public byte[] ToBytes();

        public void DeBytes(ReadOnlyMemory<byte> bytes);
    }
}
