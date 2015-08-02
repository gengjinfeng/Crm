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
            return View();
        }

        [HttpPost]
        public ActionResult Create(Order order)
        {
            return Create();
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

        public ActionResult Index()
        {
            
            return View(new Person { Name = "Foo", Gender = "F", MaritalStatus = "M", Country = new string[] { "CN", "US","UK" } });
        }

        [HttpPost]
        public ActionResult Index(Person person)
        {
            return View();
        }

        private IEnumerable<User> GetMyUser()
        {
            IEnumerable<User> items = db.User;
            User currentUser = GetCurrentUser();
            if (currentUser.Role.ROLENAME == "销售经理")
            {
                //仅能看部门内数据
                items = db.User.Where(y => y.DEPARTMENTID == currentUser.DEPARTMENTID);
            }
            else if (currentUser.Role.ROLENAME == "高级销售经理-群总" || currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
            {
                //ALL
            }
            else
            {
                //仅能看自己数据
                items = items.Where(y => y.USERID == currentUser.USERID);
            }
            return items;
        }
    } 
}
