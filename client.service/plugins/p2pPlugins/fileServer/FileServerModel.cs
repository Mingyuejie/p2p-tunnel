using ProtoBuf;
using System;

namespace client.service.plugins.p2pPlugins.fileServer
{
    [ProtoContract]
    public class FileServerModel
    {
        public FileServerModel() { }

        [ProtoMember(2)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(3)]
        public long FormId { get; set; } = 0;

        [ProtoMember(4)]
        public long ToId { get; set; } = 0;
    }

    [ProtoContract]
    public class FileServerListModel
    {
        public FileServerListModel() { }

        [ProtoMember(1)]
        public string Path { get; set; } = string.Empty;
    }


    [ProtoContract]
    public class FileServerUploadModel
    {
        public FileServerUploadModel() { }

        [ProtoMember(1)]
        public string Path { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class FileServerDownloadModel
    {
        public FileServerDownloadModel() { }

        [ProtoMember(1)]
        public string Path { get; set; } = string.Empty;
    }


    [ProtoContract]
    public class FileModel
    {
        public FileModel() { }

        [ProtoMember(3)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(4)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [ProtoMember(5)]
        public string Md5 { get; set; } = string.Empty;

        [ProtoMember(6)]
        public long Size { get; set; } = 0;

        [ProtoMember(7)]
        public FileTypes Type { get; set; } = FileTypes.UPLOAD;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum FileTypes : int
    {
        //长连接
        UPLOAD,
        //短连接
        DOWNLOAD
    }

    [ProtoContract]
    public class FileServerProgressModel
    {
        public FileServerProgressModel() { }

        [ProtoMember(2)]
        public string Md5 { get; set; } = string.Empty;

        [ProtoMember(3)]
        public long IndexLength { get; set; } = 0;
    }
}
