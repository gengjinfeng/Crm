using CRM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using PagedList;
using PagedList.Mvc;
using System.Text;

namespace CRM.Web.Controllers
{
    public class OrderController : BaseController
    {
        [HttpGet]
        public ActionResult Create()
        {
            User user = GetCurrentUser();
            IEnumerable<User> _Users = GetMyUser();
            SelectList Users = new SelectList(_Users, "USERID", "USERNAME",user.USERID);
            var usersList = Users.ToList();
            ViewBag.UserId = usersList;
            return View(new Order { CustomerStatus= "新签", Idea= "自选", CheckStatus= "未审核", Product= "仅百度", ShopStatus= "未跟进", OrderStatus= "开通" });
        }

        [HttpPost]
        public ActionResult Create(Order order)
        {
            order.CreateDate = DateTime.Now;
            order.LastModifyTime = DateTime.Now;
            db.Order.Add(order);
            db.SaveChanges();
            return Create();
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            Order model = db.Order.Where(o => o.Id == id).SingleOrDefault();
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(Order order)
        {
            try
            {
                order.LastModifyTime = DateTime.Now;
                db.Entry(order).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Content(string.Format("<script >alert('编辑失败，错误信息:{0}');window.history.go( -1 );</script >", ex.Message), "text/html");
            }
            return Content("<script >alert('编辑成功！');window.history.go( -1 );</script >", "text/html");
        }

        public ActionResult CorporationAutoComplete(long UserId,string term)
        {
            //User user = GetCurrentUser();
            string value = term.Trim();
            var cities = db.Customer.Where(c => c.PoolStatus == 1 && c.Owner == UserId);
            if (!string.IsNullOrEmpty(value))
            {
                cities = cities.Where(c => c.CorporationName.Contains(value));
            }
            var projection = from city in cities
                             where city.CorporationName.Contains(term)
                             select new
                             {
                                 id = city.CustomerID,
                                 label = city.CorporationName + "-" + city.CustomerName + "-" + city.MobileTel,
                                 value = city.CorporationName,
                                 CustomerName = city.CustomerName,
                                 Tel = city.Tel,
                                 Fax = city.Fax,
                                 Email = city.Email,
                                 Province = city.Province,
                                 City = city.City,
                                 Industry = city.Industry,
                                 MobileTel = city.MobileTel
                             };
            var lst = projection.Take(10).ToList();
            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Index(string CorporationName,string DepartmentId, string Owner, string TelePhone, string MobilePhone, string StartTime, string EndTime, int? page)
        {

            IEnumerable<Department> _Departments = GetMyDepartmentList();
            SelectList Departments = new SelectList(_Departments, "DEPARTMENTID", "DEPARTMENTNAME", DepartmentId);
            var DepartmentList = Departments.ToList();
            if (_Departments.Count() > 1)
            {
                DepartmentList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Department = DepartmentList;

            IEnumerable<User> _Users = GetMyUser(DepartmentId);
            SelectList Users = new SelectList(_Users, "USERID", "USERNAME", Owner);
            var usersList = Users.ToList();
            if (_Users.Count() > 1)
            {
                usersList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Owner = usersList;

            ViewBag.CorporationNameParams = CorporationName;
            ViewBag.DepartmentIdParams = DepartmentId;
            ViewBag.UserIdParams = Owner;
            ViewBag.TelePhoneParams = TelePhone;
            ViewBag.MobilePhoneParams = MobilePhone;

            ViewBag.StartTimeParams = StartTime;
            ViewBag.EndTimeParams = EndTime;

            var orders = from item in db.Order
                         select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                orders = orders.Where(x => x.Customer.CorporationName.Contains(CorporationName));
            }
            if (!string.IsNullOrEmpty(TelePhone))
            {
                orders = orders.Where(x => x.Customer.Tel.Contains(TelePhone));
            }
            if (!string.IsNullOrEmpty(MobilePhone))
            {
                orders = orders.Where(x => x.Customer.MobileTel.Contains(MobilePhone));
            }

            long depId = Convert.ToInt64(DepartmentId);
            if (depId > 0)
            {
                orders = orders.Where(c => c.Customer.DepartmentID == depId);
            }
            if (Owner == null)
            {
                long id = GetCurrentUser().USERID;
                orders = orders.Where(x => x.UserId == id);
            }
            if (Owner != null && Owner != "0")
            {
                long userId = Convert.ToInt64(Owner);
                orders = orders.Where(x => x.UserId == userId);
            }
            if (!string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(EndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(StartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(EndTime).ToString("yyyy-MM-dd 23:59:59"));
                orders = orders.Where(x => x.CreateDate.Value >= st && x.CreateDate <= et);
            }

            orders = orders.OrderByDescending(c => c.CreateDate);
            int pageNumber = (page ?? 1);
            return View(orders.ToPagedList(pageNumber, pageSize));
            //return View(new Person { Name = "Foo", Gender = "F", MaritalStatus = "M", Country = new string[] { "CN", "US","UK" } });
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            Order model = db.Order.Where(o => o.Id == id).SingleOrDefault();
            return View(model);
        }
    } 
}
