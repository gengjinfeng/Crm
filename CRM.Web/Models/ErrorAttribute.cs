using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.Mvc;
using NLog;
namespace CRM.Web.Models
{
    /// <summary> 
    /// 错误日志（Controller发生异常时会执行这里） 
    /// </summary> 
    public class ErrorAttribute : ActionFilterAttribute, IExceptionFilter
    {
        /// <summary> 
        /// 异常 
        /// </summary> 
        /// <param name="filterContext"></param> 
        public void OnException(ExceptionContext filterContext)
        {
            //获取异常信息，入库保存 
            Exception Error = filterContext.Exception;
            string Message = Error.Message;//错误信息 
            string Url = HttpContext.Current.Request.RawUrl;//错误发生地址 

            Logger log = LogManager.GetLogger("Mylog",typeof(ErrorAttribute));

            string error = string.Format("错误信息：{0},URL:{1},异常：{2}", Message, Url, Error.ToString());
            //处理错误消息
            log.Error(error);

            filterContext.ExceptionHandled = true;
            filterContext.Result=new ContentResult { Content = string.Format(@"出错了，错误信息：{0}",error) };
            //filterContext.Result = new RedirectResult("/Home/Error");//跳转至错误提示页面 
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
        }
    }
}