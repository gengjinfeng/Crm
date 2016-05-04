using CRM.Models;
using CRM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CRM.Web.Controllers
{
    public class AccountController : Controller
    {
        Iso58Entities db = new Iso58Entities();
        //
        // GET: /Account/

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]

        public ActionResult Validate(User param)
        {
            if (string.IsNullOrEmpty(param.USERNAME) || string.IsNullOrEmpty(param.PASSWORD))
            {
                return Content("<script >alert('用户名或者密码不能为空，请输入！');window.history.go( -1 ); </script >", "text/html");
            }

            User model =db.User.Where(u => u.USERNAME.Trim() == param.USERNAME.Trim() && u.PASSWORD.Trim() == param.PASSWORD.Trim()).SingleOrDefault();
            if (model == null)
            {
                return Content("<script >alert('登录失败，用户名或者密码错误，请输入！');window.history.go( -1 ); </script >", "text/html");
            }
            if (model.USERSTATE!= null && model.USERSTATE!="1")
            {
                return Content("<script >alert('登录失败，该用户已经禁用，不能登录系统！');window.history.go( -1 ); </script >", "text/html");
            }

            //HttpContext.Application.Add("CurrentUser", model.USERNAME);
            //HttpContext.Application.Set("CurrentUser-" + model.USERID, model.USERNAME);
            //HttpContext.Items.Add("CurrentUser", model.USERNAME);

            HttpCookie _cookie = new HttpCookie("user");
            _cookie.Values.Add("UserName", model.USERID.ToString());
            Response.Cookies.Add(_cookie);

            FormsAuthentication.SetAuthCookie(model.USERID.ToString(), true);

            FormsAuthentication.RedirectFromLoginPage(model.USERNAME.Trim(), true);
            return Redirect("/");
        }

        [HttpGet]
        public ActionResult SignOut()
        {
            HttpCookie _cookie = HttpContext.Request.Cookies["user"];
            if (_cookie != null)
            {
                //失效时间
                _cookie.Expires = DateTime.Now.AddHours(-1);
                Response.Cookies.Add(_cookie);
            }
            //HttpContext.Items.Remove("CurrentUser-" + get);
            FormsAuthentication.SignOut();
            return Redirect("/");
        }

        [HttpGet]
        public ActionResult ModifyPwd()
        {
            User user = GetCurrentUser();

            UserEntity entity = new UserEntity()
            {
                USERID = user.USERID,
                USERNAME = user.USERNAME,
                ROLEID = user.ROLEID,
                DEPARTMENTID = user.DEPARTMENTID,
                MOBILEPHONE = user.MOBILEPHONE,
                EMAIL = user.EMAIL,
                USERSTATE = user.USERSTATE,
                PHONE = user.PHONE,
                ROLE = user.Role.ROLENAME,
                DEPARTMENT = user.Department.DEPARTMENTNAME
            };
            return PartialView(entity);
        }

        [HttpPost]
        public ActionResult ModifyPwd(UserEntity model)
        {
            if (HttpContext.Request.Cookies["user"] != null)
            {
                User user = GetCurrentUser();

                if (user.PASSWORD.Trim() != model.OldPassword)
                {
                    return Content("<script >alert('旧密码错误，请输入！');window.history.go( -1 ); </script >", "text/html");
                }
                if(string.IsNullOrEmpty(model.NewPassword))
                {
                    return Content("<script >alert('新密码不能为空，请输入！');window.history.go( -1 ); </script >", "text/html");
                }

                user.PASSWORD = model.NewPassword.Trim();
                db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Content("<script >alert('密码修改成功！');window.history.go( -1 ); </script >", "text/html");
            }
            return new RedirectResult("/Account/Login");
        }

        public User GetCurrentUser()
        {
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
