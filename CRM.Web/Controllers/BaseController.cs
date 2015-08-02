using CRM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

using System.Runtime.Caching;
namespace CRM.Web.Controllers
{
    public class BaseController : Controller
    {
        protected Iso58Entities db = new Iso58Entities();

        public int pageSize = 50;

        //public BaseController()
        //{
        //    HttpCookie cookie = HttpContext.Request.Cookies["PageSize"];
        //    if (cookie != null)
        //    {
        //        pageSize = Convert.ToInt32(cookie.Value);
        //    }
        //}
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.Cookies["user"] == null)
            {
                if (this.RouteData.Values["Controller"].ToString() != "Account")
                {
                    //跳转到登陆页
                    filterContext.Result = new RedirectResult("/Account/Login");
                }
            }
            base.OnActionExecuting(filterContext);
        }

        public User GetCurrentUser()
        {
            //ObjectCache cache = System.Runtime.Caching.MemoryCache.Default;
            //if (HttpContext.Request.Cookies["user"] != null)
            //{
            //    HttpCookie cookie = HttpContext.Request.Cookies["user"];
            //    long UserId = Convert.ToInt64(cookie.Values["UserName"]);// Convert.ToInt64(cookie.Values["UserId"]);
            //    User current=cache.Get(UserId.ToString()) as User;
            //    if(current==null)
            //    {
            //        var items = from item in db.User
            //                    where item.USERID == UserId
            //                    select item;
            //        User user = items.FirstOrDefault();
            //        current = user;
            //        CacheItemPolicy policy = new CacheItemPolicy();
            //        policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(3600*12);
            //        cache.Set(UserId.ToString(), current, policy);
            //        return user;
            //    }
            //    return current;
            //}
            //return null;


            if (HttpContext.Request.Cookies["user"] != null)
            {
                HttpCookie cookie = HttpContext.Request.Cookies["user"];
                long UserId = Convert.ToInt64(cookie.Values["UserName"]);// Convert.ToInt64(cookie.Values["UserId"]);
                var items = from item in db.User
                            where item.USERID == UserId
                            select item;
                User user = items.FirstOrDefault();
                return user;
            }
            return null;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}