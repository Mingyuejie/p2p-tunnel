using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.db
{
    [Table("CategoryInfo")]
    public class CategoryInfo
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public long AddTime { get; set; } = 0;
    }
}
