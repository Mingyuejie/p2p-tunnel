using client.service.p2pPlugins.plugins.request;
using ProtoBuf;
using System;

namespace client.service.p2pPlugins.plugins.fileServer
{

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum P2PFileCmdTypes : int
    {
        LIST, //发送列表
        DOWNLOAD,//请求下载
        UPLOAD,//请求下载
        FILE //发送文件
    }

    [ProtoContract]
    public class P2PFileModel : IP2PMessageBase
    {
        public P2PFileModel() { }

        [ProtoMember(1, IsRequired = true)]
        public P2PDataMessageTypes Type { get; } = P2PDataMessageTypes.FILE_SERVER;

        [ProtoMember(2)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(3)]
        public long FormId { get; set; } = 0;

        [ProtoMember(4)]
        public long ToId { get; set; } = 0;

        [ProtoMember(5)]
        public P2PFileCmdTypes CmdType { get; set; } = P2PFileCmdTypes.DOWNLOAD;
    }

    [ProtoContract]
    public class P2PFileCmdListModel : IRequestExcuteMessage
    {
        public P2PFileCmdListModel() { }

        [ProtoMember(1)]
        public string Path { get; set; } = string.Empty;

    }

    [ProtoContract]
    public class P2PFileCmdUploadModel
    {
        public P2PFileCmdUploadModel() { }

        [ProtoMember(1, IsRequired = true)]
        public P2PFileCmdTypes CmdType { get; } = P2PFileCmdTypes.UPLOAD;

        [ProtoMember(2)]
        public string Path { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class P2PFileCmdDownloadModel
    {
        public P2PFileCmdDownloadModel() { }

        [ProtoMember(1, IsRequired = true)]
        public P2PFileCmdTypes CmdType { get; } = P2PFileCmdTypes.DOWNLOAD;

        [ProtoMember(2)]
        public string Path { get; set; } = string.Empty;
    }


    [ProtoContract]
    public class P2PFileFileModel
    {
        public P2PFileFileModel() { }

        [ProtoMember(1, IsRequired = true)]
        public P2PFileCmdTypes CmdType { get; } = P2PFileCmdTypes.FILE;

        [ProtoMember(2, IsRequired = true)]
        public P2PFileCmdTypes FileType { get; set; } = P2PFileCmdTypes.DOWNLOAD;

        [ProtoMember(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(5)]
        public string Md5 { get; set; } = string.Empty;

        [ProtoMember(6)]
        public long Size { get; set; } = 0;
    }
}
