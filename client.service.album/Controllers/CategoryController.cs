using client.service.album.db;
using client.service.album.filters;
using common;
using common.extends;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album.Controllers
{
    public class CategoryController : BaseController
    {
        private readonly DBHelper<CategoryInfo> dbHelper;
        private readonly VerifyCaching verifyCaching;
        public CategoryController(DBHelper<CategoryInfo> dbHelper, VerifyCaching verifyCaching)
        {
            this.dbHelper = dbHelper;
            this.verifyCaching = verifyCaching;
        }

        [HttpGet]
        public async Task<IEnumerable<CategoryInfo>> List()
        {
            var res = await dbHelper.GetAll();
            return res;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CategoryInfo model)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }

            var old = await dbHelper.Get(model.ID) ?? new CategoryInfo { ID = 0 };
            model.FormatObjectAttr();
            if (old.ID == 0)
            {
                model.AddTime = Helper.GetTimeStampSec();
                return new ObjectResult(await dbHelper.Add(model));
            }
            await dbHelper.Update(model);
            return new ObjectResult(old.ID);
        }

        [HttpPost]
        public async Task<IActionResult> Del([FromQuery] string ids)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }

            await dbHelper.Execute($"DELETE {typeof(CategoryInfo).Name} WHERE ID in @ids", new { ids = ids.ToIntArray() });
            await dbHelper.Execute($"DELETE {typeof(AlbumInfo).Name} WHERE CID in @ids", new { ids = ids.ToIntArray() });
            return new ObjectResult(true);
        }
    }
}
