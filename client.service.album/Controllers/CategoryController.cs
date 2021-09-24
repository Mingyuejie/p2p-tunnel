using client.service.album.db;
using client.service.album.filters;
using common;
using common.extends;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.Controllers
{
    public class CategoryController : BaseController
    {
        private readonly DBHelper<CategoryInfo> dbHelper;
        private readonly DBHelper<AlbumInfo> dbAlbumHelper;
        private readonly VerifyCaching verifyCaching;
        private readonly IWebHostEnvironment webHostEnvironment;
        public CategoryController(DBHelper<CategoryInfo> dbHelper, DBHelper<AlbumInfo> dbAlbumHelper, VerifyCaching verifyCaching, IWebHostEnvironment webHostEnvironment)
        {
            this.dbHelper = dbHelper;
            this.dbAlbumHelper = dbAlbumHelper;
            this.verifyCaching = verifyCaching;
            this.webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<PageWrap<IEnumerable<CategoryInfo>>> List(int p = 1, int ps = 10)
        {
            int count = await dbHelper.QueryCount();
            IEnumerable<CategoryInfo> res = await dbHelper.Query(
                order: "order by AddTime desc",
                limit: "limit @ps offset @skip",
                param: new { ps, skip = (p - 1) * ps });
            return new PageWrap<IEnumerable<CategoryInfo>>
            {
                Page = p,
                PageSize = ps,
                Data = res,
                Count = count
            };
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CategoryAddInfo param)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }
            param.FormatObjectAttr();
            CategoryInfo model = new CategoryInfo { Name = param.Name, Cover = param.Cover, AddTime = Helper.GetTimeStampSec() };
            return new ObjectResult(await dbHelper.Add(model));
        }

        [HttpPost]
        public async Task<IActionResult> EditName([FromBody] CategoryEditNameInfo param)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }
            param.FormatObjectAttr();
            int resullt = await dbHelper.Update($"Name=@name", "ID=@id", new { name = param.Name, id = param.ID });
            return new ObjectResult(resullt);
        }

        [HttpPost]
        public async Task<IActionResult> EditCover([FromBody] CategoryEditCoverInfo param)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }
            param.FormatObjectAttr();
            int resullt = await dbHelper.Update("Cover=@cover", "ID=@id", new { cover = param.Cover, id = param.ID });
            return new ObjectResult(resullt);
        }


        [HttpPost]
        public async Task<IActionResult> Del([FromQuery] string ids)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }

            await dbHelper.Delete($"ID in @ids", new { ids = ids.ToIntArray() });

            IEnumerable<AlbumInfo> res = await dbAlbumHelper.Query(
              where: " AND ID in @ids",
              param: new { ids = ids.ToIntArray() });
            foreach (var item in res)
            {
                string path = Path.Join(webHostEnvironment.WebRootPath, item.Path);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            await dbAlbumHelper.Delete($"CID in @ids", new { ids = ids.ToIntArray() });
            return new ObjectResult(true);
        }
    }
}
