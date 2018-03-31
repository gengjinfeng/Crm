using CRM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

using System.Runtime.Caching;
using CRM.Web.Models;
namespace CRM.Web.Controllers
{
    [ErrorAttribute]
    public class BaseController : Controller
    {
        protected Iso58Entities db = new Iso58Entities();

        public int pageSize = 30;

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
                            where item.USERID == UserId && item.USERSTATE=="1"
                            select item;
                User user = items.FirstOrDefault();
                return user;
            }
            return null;
        }

        public void InitPageSize()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["PageSize"];
            if (cookie != null)
            {
                pageSize = Convert.ToInt32(cookie.Value);
            }
        }

        public IEnumerable<Department> GetMyDepartmentList()
        {
            IEnumerable<Department> items = db.Department;
            User currentUser = GetCurrentUser();
            if (currentUser.Role.ROLENAME == "销售经理")
            {
                //仅能看部门内数据
                items = db.Department.Where(y => y.DEPARTMENTID == currentUser.DEPARTMENTID);
            }
            else if (currentUser.Role.ROLENAME == "高级销售经理-群总")
            {
                //看权限设置的部门数据
                
                var authDeps = from auth in db.Authority
                               where auth.UserId == currentUser.USERID
                               select auth.DepartmentId;
                if(authDeps.Count()>0)
                {
                    items = db.Department.Where(d => authDeps.Contains(d.DEPARTMENTID));
                }
                else
                {
                    items = db.Department.Where(y => y.DEPARTMENTID == currentUser.DEPARTMENTID);
                }
            }
            else if (currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
            {
                //全部数据
            }
            else
            {
                //仅能看自己数据
                items = items.Where(y => y.DEPARTMENTID == currentUser.DEPARTMENTID);
            }
            return items;
        }

        public IEnumerable<User> GetMyUser(string departmentId = "0")
        {
            IEnumerable<User> items = db.User.Where(u=>u.USERSTATE=="1");
            User currentUser = GetCurrentUser();
            if (currentUser.Role.ROLENAME == "销售经理")
            {
                //仅能看部门内数据
                items = items.Where(y => y.DEPARTMENTID == currentUser.DEPARTMENTID);
            }
            else if (currentUser.Role.ROLENAME == "高级销售经理-群总")
            {
                //看权限设置的部门数据
                
                var authDeps = from auth in db.Authority
                               where auth.UserId == currentUser.USERID
                               select auth.DepartmentId;
                if (authDeps.Count() > 0)
                {
                    items = items.Where(d => authDeps.Contains(d.DEPARTMENTID));
                }
                else
                {
                    long depId = Convert.ToInt64(departmentId);
                    if (depId > 0)
                    {
                        items = items.Where(y => y.DEPARTMENTID == depId);
                    }
                }
            }
            else if (currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
            {
                long depId = Convert.ToInt64(departmentId);
                if (depId > 0)
                {
                    items = items.Where(y => y.DEPARTMENTID == depId);
                }
            }
            else
            {
                //仅能看自己数据
                items = items.Where(y => y.USERID == currentUser.USERID);
            }
            return items;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


        public bool IsRole
        {
            get
            {
                User currentUser = GetCurrentUser();
                if (currentUser.Role.ROLENAME == "销售经理")
                {
                    return true;
                }
                else if (currentUser.Role.ROLENAME == "高级销售经理-群总" || currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
                {
                    return true;
                }
                else
                {
                    //仅能看自己数据
                    return false;
                }
            }
        }

        public bool IsDirectorRole
        {
            get
            {
                User currentUser = GetCurrentUser();
                if (currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
                {
                    return true;
                }
                else
                {
                    //仅能看自己数据
                    return false;
                }
            }
        }
    }
}