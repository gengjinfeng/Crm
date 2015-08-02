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
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            //User user = GetCurrentUser();
            //ViewBag.CurrentUser = user.USERNAME;
            //string CurrentUserRole = user.Role.ROLENAME;
            //ViewBag.IsDirector = ((CurrentUserRole == "销售总监") || (CurrentUserRole == "高级管理员"));
            return View();
        }

        public ActionResult Statics(StaticsSearchParams param)
        {
            ViewBag.IsRole = IsRole;
            User Current = GetCurrentUser();
            var statusSelectList = GetStatusList(param.Status).ToList();
            statusSelectList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            ViewBag.Status = statusSelectList;

            //IEnumerable<User> _Users = GetMyUser();
            if (string.IsNullOrEmpty(param.Owner))
            {
                param.Owner = Current.USERID.ToString();
            }


            IEnumerable<Department> _Departments = GetMyDepartmentList();
            SelectList Departments = new SelectList(_Departments, "DEPARTMENTID", "DEPARTMENTNAME", param.DepartmentId);
            var DepartmentList = Departments.ToList();
            if (_Departments.Count() > 1)
            {
                DepartmentList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Department = DepartmentList;

            IEnumerable<User> _Users = GetMyUser(param.DepartmentId);
            SelectList Users = new SelectList(_Users, "USERID", "USERNAME", param.Owner);
            var usersList = Users.ToList();
            if (_Users.Count() > 1)
            {
                usersList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Owner = usersList;


            //SelectList Users = new SelectList(_Users, "USERID", "USERNAME", param.Owner);
            //var usersList = Users.ToList();
            //if (_Users.Count() > 1)
            //{
            //    usersList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            //}
            //ViewBag.Owner = usersList;

            ViewBag.NextContractStartTimeParams = param.NextContractStartTime;
            ViewBag.NextContractEndTimeParams = param.NextContractEndTime;

            ViewBag.CreateDateStartTimeParams = param.CreateDateStartTime;
            ViewBag.CreateDateEndTimeParams = param.CreateDateEndTime;

            string sql = @"select U.USERNAME, [Status],COUNT(0) Total from [dbo].[Customer] as  c
                                    inner join [dbo].[User] as u
                                    on c.[Owner]=u.USERID
                                    where  [PoolStatus]=1 {0}
                                    group by U.USERNAME, [Status]
                                    order by u.username,[Status],total desc";
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(param.Status) && param.Status != "0")
            {
                sb.AppendFormat(" and c.[Status]='{0}' ", param.Status);
            }
            if (!string.IsNullOrEmpty(param.Owner) && param.Owner != "0")
            {
                sb.AppendFormat(" and c.[Owner]={0} ", param.Owner);
            }
            if (!string.IsNullOrEmpty(param.NextContractStartTime) && !string.IsNullOrEmpty(param.NextContractEndTime))
            {
                sb.AppendFormat(" and c.[NextContactTime] >='{0} 00:00:00' and c.[NextContactTime]<='{1} 23:59:59' ", Convert.ToDateTime(param.NextContractStartTime).ToString("yyyy-MM-dd"), Convert.ToDateTime(param.NextContractEndTime).ToString("yyyy-MM-dd"));
            }
            if (!string.IsNullOrEmpty(param.CreateDateStartTime) && !string.IsNullOrEmpty(param.CreateDateEndTime))
            {
                sb.AppendFormat(" and c.[CreateDate] >='{0} 00:00:00' and c.[CreateDate]<='{1} 23:59:59' ", Convert.ToDateTime(param.CreateDateStartTime).ToString("yyyy-MM-dd"), Convert.ToDateTime(param.CreateDateEndTime).ToString("yyyy-MM-dd"));
            }
            if (Current.Role.ROLENAME == "销售经理")
            {
                //仅能看部门内数据
                sb.AppendFormat(" and  c.[DepartmentID]={0} ", Current.DEPARTMENTID);
            }
            else if (Current.Role.ROLENAME == "高级销售经理-群总" || Current.Role.ROLENAME == "销售总监" || Current.Role.ROLENAME == "高级管理员")
            {
                long depId = Convert.ToInt64(param.DepartmentId);
                if (depId > 0)
                {
                    sb.AppendFormat(" and  c.[DepartmentID]={0} ", param.DepartmentId);
                }
            }
            else
            {
                sb.AppendFormat(" and  c.[DepartmentID]={0} ", Current.DEPARTMENTID);
            }
            sql = string.Format(sql, sb.ToString());

            var RecordItem = db.Database.SqlQuery<Statics>(sql, new object[]{});


            string sumSQL = @"select U.USERNAME,COUNT(0) Total from [dbo].[Customer] as  c
                                    inner join [dbo].[User] as u
                                    on c.[Owner]=u.USERID
                                    where  [PoolStatus]=1 {0} 
                                    group by U.USERNAME
                                    order by u.username,total desc";
            sumSQL = string.Format(sumSQL, sb.ToString());
            var RecordTotal = db.Database.SqlQuery<Statics>(sumSQL, new object[] { });

            CRMStatics items = new CRMStatics() { RecordItem = RecordItem, RecordTotal = RecordTotal };
            return View(items);
        }

        public ActionResult SetPageSize()
        {
            HttpCookie cookie = HttpContext.Request.Cookies["PageSize"];
            ViewBag.pageSize = 50;
            if (cookie != null)
            {
                ViewBag.pageSize = Convert.ToInt32(cookie.Value);
            }
            return View();
        }
        [HttpPost]
        public ActionResult SetPageSize(Int64 PageSize)
        {
            try
            {
                HttpCookie cookie = HttpContext.Request.Cookies["PageSize"];
                if (cookie != null)
                {
                    cookie.Values.Remove("PageSize");
                    cookie.Expires = DateTime.Now.AddHours(-1);
                    Response.Cookies.Add(cookie);
                }

                HttpCookie acookie = new HttpCookie("PageSize");
                acookie.Value = PageSize.ToString();
                acookie.Expires = DateTime.MaxValue;
                Response.Cookies.Add(acookie);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "编辑失败，原因:" + ex.Message });
            }
            return Json(new { success = true });
        }

        public ActionResult Contact()
        {
            return View();
        }

        private SelectList GetStatusList(object selectedValue)
        {
            List<string> lst = new List<string>()
            {
               "0类",
               "1类",
               "2类",
               "普3类",
               "重3类",
               "4类",
               "5类",
               "6类",
               "9类",
               "已联系",
               "错误信息",
               "不是收费会员",
               "无合作意向"
            };
            SelectList statusList = null;
            if (selectedValue != null && selectedValue != string.Empty)
            {
                statusList = new SelectList(lst, selectedValue);
            }
            else
            {
                statusList = new SelectList(lst);
            }
            return statusList;
        }

        private IEnumerable<User> GetMyUser(string departmentId = "0")
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
                long depId = Convert.ToInt64(departmentId);
                if (depId > 0)
                {
                    items = db.User.Where(y => y.DEPARTMENTID == depId);
                }
            }
            else
            {
                //仅能看自己数据
                items = items.Where(y => y.USERID == currentUser.USERID);
            }
            return items;
        }

        private IEnumerable<Department> GetMyDepartmentList()
        {
            IEnumerable<Department> items = db.Department;
            User currentUser = GetCurrentUser();
            if (currentUser.Role.ROLENAME == "销售经理")
            {
                //仅能看部门内数据
                items = db.Department.Where(y => y.DEPARTMENTID == currentUser.DEPARTMENTID);
            }
            else if (currentUser.Role.ROLENAME == "高级销售经理-群总" || currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
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
    }
}
