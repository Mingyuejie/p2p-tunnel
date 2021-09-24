using common.extends;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace client.service.album.filters
{
    /// <summary>
    /// api返回格式中间件
    /// </summary>
    public class WebApiResultMiddleware : ActionFilterAttribute
    {
        public WebApiResultMiddleware()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (!context.ModelState.IsValid)
            {
                List<string> result = new();

                foreach (string key in context.ModelState.Keys.ToList())
                {
                    foreach (var error in context.ModelState[key].Errors)
                    {
                        result.Add(error.ErrorMessage);
                    }
                }
                context.Result = new JsonResult(new JsonResultModel { Data = { }, Msg = string.Join(",", result), Code = 415 });
            }
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is OkObjectResult || context.Result is ViewResult || context.Result is EmptyResult)
            {


            }
            else if (context.Result is BadRequestObjectResult)
            {
                BadRequestObjectResult objectResult = context.Result as BadRequestObjectResult;
                context.Result = new JsonResult(new JsonResultModel { Data = objectResult.Value.ToJson(), Msg = objectResult.StatusCode.ToString(), Code = 500 });
            }

            else if (context.Result is BadRequestResult)
            {
                BadRequestResult objectResult = context.Result as BadRequestResult;
                context.Result = new JsonResult(new JsonResultModel { Data = objectResult.ToJson(), Msg = objectResult.StatusCode.ToString(), Code = 500 });
            }

            else if (context.Result is ObjectResult)
            {
                ObjectResult objectResult = context.Result as ObjectResult;
                if (objectResult.Value == null)
                {
                    context.Result = new JsonResult(new JsonResultModel { Data = objectResult.Value });
                }
                else
                {
                    context.Result = new JsonResult(new JsonResultModel { Data = objectResult.Value });
                }
            }
            else if (context.Result is BoolResult)
            {
                BoolResult result = context.Result as BoolResult;

                context.Result = new JsonResult(new JsonResultModel { Data = result.Content, });
            }
            else if (context.Result is OriginResult)
            {
                OriginResult result = context.Result as OriginResult;

                context.Result = new JsonResult(result.Content);

            }
            else if (context.Result is XmlResult)
            {
                XmlResult objectResult = context.Result as XmlResult;
                context.Result = new ContentResult
                {
                    Content = objectResult.Content
                };
            }
            else if (context.Result is ErrorResult)
            {
                ErrorResult objectResult = context.Result as ErrorResult;
                context.Result = new JsonResult(new JsonResultModel { Msg = objectResult.Content, Data = objectResult.Data, Code = -1 });
            }
            else if (context.Result is ContentResult)
            {
                context.Result = new JsonResult(new JsonResultModel { Data = (context.Result as ContentResult).Content });
            }
            else if (context.Result is StatusCodeResult)
            {
                context.Result = new JsonResult(new JsonResultModel { Msg = (context.Result as StatusCodeResult).StatusCode.ToString() });
            }
        }
    }

    public class JsonResultModel
    {
        public int Code { get; set; } = 0;
        public string Msg { get; set; } = "";
        public object Data { get; set; } = string.Empty;
    }

    public class BoolResult : ActionResult
    {

        public bool Content { get; set; }

        public BoolResult()
        {

        }

        public override void ExecuteResult(ActionContext context)
        {
            Content = context.HttpContext.Response.Body.ToString().Convert<bool>();
        }
    }

    public class OriginResult : ActionResult
    {

        public object Content { get; set; }

        public OriginResult()
        {

        }

        public override void ExecuteResult(ActionContext context)
        {
            Content = context.HttpContext.Response.Body.ToString();
        }
    }

    public class IntResult : ActionResult
    {

        public int Num { get; set; }

        public IntResult(int num)
        {
            Num = num;
        }
        public override void ExecuteResult(ActionContext context)
        {
            Num = Convert.ToInt32(context.HttpContext.Response.Body.ToString());
        }
    }

    public class ErrorResult : ActionResult
    {

        public string Content { get; set; }
        public object Data { get; set; } = string.Empty;

        public ErrorResult(string content)
        {
            Content = content;
        }

        public ErrorResult(object data)
        {
            Data = data;
        }

        public ErrorResult(string content, object data)
        {
            Content = content;
            Data = data;
        }

        public override void ExecuteResult(ActionContext context)
        {
            Content = context.HttpContext.Response.Body.ToString();
        }
    }

    public class XmlResult : ActionResult
    {

        public string Content { get; set; }

        public XmlResult()
        {

        }

        public override void ExecuteResult(ActionContext context)
        {
            Content = context.HttpContext.Response.Body.ToString();
        }
    }

}
