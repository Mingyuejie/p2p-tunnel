using client.service.p2pPlugins.plugins.request;
using ProtoBuf;
using System;

namespace client.service.p2pPlugins.plugins.fileServer
{

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FileServerCmdTypes : int
    {
        LIST, //发送列表
        DOWNLOAD,//请求下载
        UPLOAD,//请求下载
        FILE, //发送文件
        PROGRESS, //进度
    }

    [ProtoContract]
    public class FileServerModel : IP2PMessageBase
    {
        public FileServerModel() { }

        [ProtoMember(1, IsRequired = true)]
        public P2PDataTypes Type { get; } = P2PDataTypes.FILE_SERVER;

        [ProtoMember(2)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(3)]
        public long FormId { get; set; } = 0;

        [ProtoMember(4)]
        public long ToId { get; set; } = 0;

        [ProtoMember(5)]
        public FileServerCmdTypes CmdType { get; set; } = FileServerCmdTypes.DOWNLOAD;
    }

    [ProtoContract]
    public class FileServerListModel : IRequestExcuteMessage
    {
        public FileServerListModel() { }

        [ProtoMember(1)]
        public string Path { get; set; } = string.Empty;

    }

    [ProtoContract]
    public class FileServerUploadModel
    {
        public FileServerUploadModel() { }

        [ProtoMember(1, IsRequired = true)]
        public FileServerCmdTypes CmdType { get; } = FileServerCmdTypes.UPLOAD;

        [ProtoMember(2)]
        public string Path { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class FileServerDownloadModel
    {
        public FileServerDownloadModel() { }

        [ProtoMember(1, IsRequired = true)]
        public FileServerCmdTypes CmdType { get; } = FileServerCmdTypes.DOWNLOAD;

        [ProtoMember(2)]
        public string Path { get; set; } = string.Empty;
    }


    [ProtoContract]
    public class FileModel
    {
        public FileModel() { }

        [ProtoMember(1, IsRequired = true)]
        public FileServerCmdTypes CmdType { get; } = FileServerCmdTypes.FILE;

        [ProtoMember(2, IsRequired = true)]
        public FileServerCmdTypes FileType { get; set; } = FileServerCmdTypes.DOWNLOAD;

        [ProtoMember(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(5)]
        public string Md5 { get; set; } = string.Empty;

        [ProtoMember(6)]
        public long Size { get; set; } = 0;
    }

    [ProtoContract]
    public class FileServerProgressModel
    {
        public FileServerProgressModel() { }

        [ProtoMember(1, IsRequired = true)]
        public FileServerCmdTypes CmdType { get; } = FileServerCmdTypes.PROGRESS;

        [ProtoMember(2)]
        public string Md5 { get; set; } = string.Empty;

        [ProtoMember(3)]
        public long IndexLength { get; set; } = 0;
    }
}
