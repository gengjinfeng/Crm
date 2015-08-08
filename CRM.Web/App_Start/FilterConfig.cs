using System.Web.Mvc;

using NLog;
namespace CRM.Web.App_Start
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }

    public class MyExceptionFileAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            //OnException(filterContext);
            

            Logger log = LogManager.GetLogger("MyLog",typeof(FilterConfig));

            //处理错误消息，将其跳转到一个页面
            log.Error(filterContext.Exception.ToString());

            //页面跳转到错误页面
            filterContext.HttpContext.Response.Redirect("/Home/Error");
        }
    }
}