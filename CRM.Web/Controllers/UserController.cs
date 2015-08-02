using CRM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CRM.Web.Controllers
{
    public class UserController : BaseController
    {
        //
        public ActionResult Index(string userName)
        {
            var items = from u in db.User
                        select u;
            if (!string.IsNullOrEmpty(userName))
            {
                string _username = userName.Trim();
                items = items.Where(u => u.USERNAME.Contains(_username));
            }
            return View(items);
        }

        //
        // GET: /User/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /User/Create
        public ActionResult Create()
        {
            var roles = from r in db.Role
                        select new { RoleId = r.ROLEID, RoleName = r.ROLENAME };
            var departments = from d in db.Department
                              select new { DepartmentId = d.DEPARTMENTID, DepartmentName = d.DEPARTMENTNAME };
            ViewBag.Roles = new SelectList(roles,"RoleId","RoleName");
            ViewBag.Departments = new SelectList(departments,"DepartmentId","DepartmentName");
            return View();
        }

        //
        // POST: /User/Create
        [HttpPost]
        public ActionResult Create(User value)
        {
            try
            {
                // TODO: Add insert logic here
                value.USERSTATE = true;
                db.User.Add(value);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content(string.Format("<script >alert('添加失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
            }
        }

        //
        // GET: /User/Edit/5
        public ActionResult Edit(long id)
        {
            User user = db.User.Where(u => u.USERID == id).SingleOrDefault();

            var roles = from r in db.Role
                        select new { RoleId = r.ROLEID, RoleName = r.ROLENAME };
            var departments = from d in db.Department
                              select new { DepartmentId = d.DEPARTMENTID, DepartmentName = d.DEPARTMENTNAME };
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName",user.ROLEID);
            ViewBag.Departments = new SelectList(departments, "DepartmentId", "DepartmentName", user.DEPARTMENTID);
            return View(user);
        }

        //
        // POST: /User/Edit/5
        [HttpPost]
        public ActionResult Edit(User entity)
        {
            try
            {
                db.Entry(entity).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                var items=db.Customer.Where(c => c.Owner == entity.USERID && c.DepartmentID!=entity.DEPARTMENTID);
                foreach (var item in items)
                {
                    item.DepartmentID = entity.DEPARTMENTID;
                    db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                return Content(string.Format("<script >alert('编辑失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
            }
            return Content("<script >alert('编辑成功！');window.opener=null;window.close(); </script >", "text/html");
        }

        //
        // POST: /User/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(long id)
        {
            try
            {
                User model = db.User.Find(id);
                db.User.Remove(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content(string.Format("<script >alert('删除失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
            }
        }

        public ActionResult ModifyUserState(long id, int UserState)
        {
            try
            {
                User model = db.User.Find(id);
                model.USERSTATE = (UserState > 0 ? true : false);
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content(string.Format("<script >alert('修改失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
            }
        }

        public ActionResult ResetPassword(long id)
        {
            var user = db.User.Where(u => u.USERID == id).SingleOrDefault();
            if(user!=null)
            {
                user.PASSWORD = "123456";
                db.SaveChanges();
            }
            return Content("<script >alert('密码重置成功！');window.history.go( -1 ); </script >", "text/html");
        }
    }
}
