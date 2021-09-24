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
    public class AlbumController : BaseController
    {
        private readonly DBHelper<AlbumInfo> dbHelper;
        private readonly VerifyCaching verifyCaching;
        private readonly IWebHostEnvironment webHostEnvironment;

        public AlbumController(DBHelper<AlbumInfo> dbHelper, VerifyCaching verifyCaching, IWebHostEnvironment webHostEnvironment)
        {
            this.dbHelper = dbHelper;
            this.verifyCaching = verifyCaching;
            this.webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<PageWrap<IEnumerable<AlbumInfo>>> List([FromQuery] int cid = 0, int p = 1, int ps = 10)
        {
            int count = await dbHelper.QueryCount();
            IEnumerable<AlbumInfo> res = await dbHelper.Query(
                where: "AND CID=@cid",
                order: "order by AddTime desc",
                limit: "limit @ps offset @skip",
                param: new { cid, ps, skip = (p - 1) * ps });
            return new PageWrap<IEnumerable<AlbumInfo>>
            {
                Page = p,
                PageSize = ps,
                Data = res,
                Count = count
            };
        }

        [HttpPost]
        public async Task<IActionResult> Add()
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }

            var fileData = Request.Form.Files["file"];
            var allowTypes = new string[] { "image/png", "image/jpg", "image/jpeg", "image/gif" };
            if (!allowTypes.Contains(fileData.ContentType))
            {
                return new ErrorResult($"只允许上传:{string.Join(",", allowTypes)}");
            }
            int cid = Request.Form["cid"].ToString().Convert<int>();

            string savePath = Path.Combine(webHostEnvironment.WebRootPath, "album", cid.ToString());
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            string saveName = Path.Combine(savePath, fileData.FileName);
            using FileStream fs = new FileStream(saveName, FileMode.Create);
            fileData.CopyTo(fs);
            fs.Close();

            AlbumInfo model = new AlbumInfo
            {
                AddTime = Helper.GetTimeStampSec(),
                CID = cid,
                Name = Path.GetFileNameWithoutExtension(fileData.FileName),
                Path = saveName.Replace(webHostEnvironment.WebRootPath, "").Replace('\\', '/'),
                Remark = string.Empty
            };
            return new ObjectResult(await dbHelper.Add(model));
        }

        [HttpPost]
        public async Task<IActionResult> EditName([FromBody] AlbumEditNameInfo param)
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
        public async Task<IActionResult> Del([FromQuery] string ids)
        {
            if (!verifyCaching.Verify(Request))
            {
                return new ErrorResult("身份未验证");
            }
            IEnumerable<AlbumInfo> res = await dbHelper.Query(
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

            await dbHelper.Delete($"ID in @ids", new { ids = ids.ToIntArray() });
            return new ObjectResult(true);
        }
    }
}
