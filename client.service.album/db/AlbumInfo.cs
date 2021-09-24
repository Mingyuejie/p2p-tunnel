using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.db
{
    [Table("AlbumInfo")]
    public class AlbumInfo
    {
        [Dapper.Contrib.Extensions.Key]
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public long AddTime { get; set; } = 0;
        public int CID { get; set; } = 0;
    }
}
