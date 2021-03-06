﻿using CRM.Repository;
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
        public ActionResult Index(string userName,string DepartmentId)
        {
            var items = from u in db.User
                        select u;
            if (!string.IsNullOrEmpty(userName))
            {
                string _username = userName.Trim();
                items = items.Where(u => u.USERNAME.Contains(_username));
            }


            var departments = from d in db.Department
                              select new { DepartmentId = d.DEPARTMENTID, DepartmentName = d.DEPARTMENTNAME };
            SelectList list=new SelectList(departments, "DepartmentId", "DepartmentName");
            if (!string.IsNullOrEmpty(DepartmentId) && DepartmentId != "0")
            {
                int depid= int.Parse(DepartmentId);
                list= new SelectList(departments, "DepartmentId", "DepartmentName", DepartmentId);

                items = items.Where(u => u.DEPARTMENTID == depid);
            }
            ViewBag.Departments = list;

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
                value.USERSTATE = "1";
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
                    db.SaveChanges();
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
                var items=db.Customer.Where(c => c.Owner == id && c.PoolStatus.Value>1);
                foreach (var item in items)
                {
                    item.Owner = 35;//gengjinfeng
                    db.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }

                User model = db.User.Find(id);
                db.User.Remove(model);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败：" + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ModifyUserState(long id, int UserState)
        {
            try
            {
                User model = db.User.Find(id);
                model.USERSTATE = UserState.ToString();
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true });
                //return RedirectToAction("Index");
                //return Content("<script >alert('修改成功！');window.history.go( -1 ); </script >", "text/html");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败：" + ex.Message });
                //return Content(string.Format("<script >alert('修改失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
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

        
        public ActionResult Authority(long UserId)
        {
            ViewBag.UserId = UserId;
            var user = db.User.Where(u => u.USERID == UserId).SingleOrDefault();
            if (user != null)
            {
                ViewBag.UserName = user.USERNAME;
            }
            return View();
        }
        [HttpPost]
        public ActionResult Authority(long UserId,string DepartmentIds)
        {
            try
            {


                var auts = db.Authority.Where(au => au.UserId.Value == UserId);
                if (auts.Count() > 0)
                {
                    foreach (var item in auts)
                    {
                        db.Authority.Remove(item);
                        db.SaveChanges();
                    }
                }

                string[] depIds = DepartmentIds.Split(new char[] { ',' });
                foreach (var item in depIds)
                {
                    db.Authority.Add(new Repository.Authority() {
                        UserId = UserId,
                        DepartmentId = long.Parse(item)
                    });
                    db.SaveChanges();
                }
                return Json(new { success = true });
                //return RedirectToAction("Index");
                //return Content("<script >alert('修改成功！');window.history.go( -1 ); </script >", "text/html");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败：" + ex.Message });
                //return Content(string.Format("<script >alert('修改失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
            }
        }
    }
}
