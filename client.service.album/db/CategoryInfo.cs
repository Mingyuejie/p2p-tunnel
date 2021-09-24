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
        [Dapper.Contrib.Extensions.Key]
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public long AddTime { get; set; } = 0;
        public string Cover { get; set; } = string.Empty;
    }

    public class CategoryAddInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Cover { get; set; } = string.Empty;
    }

    public class CategoryEditNameInfo
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryEditCoverInfo
    {
        public int ID { get; set; }
        public string Cover { get; set; } = string.Empty;
    }
}
