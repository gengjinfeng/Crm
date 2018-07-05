using CRM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using PagedList;
using PagedList.Mvc;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using System.IO;

namespace CRM.Web.Controllers
{
    public class CustomerController : BaseController
    {
        private const int privateTag = 1;//私有
        private const int publicTag = 2;//公共池
        private const int obsoleteTag = 3;//废弃池

        public ActionResult Index(string CorporationName,string fromType,string DepartmentId,string Owner,string status,string NextContractStartTime,string NextContractEndTime,string TelePhone,string MobilePhone,string UpdateStartTime,string UpdateEndTime,int? page)
        {

            InitPageSize();

            ViewBag.IsRole = IsRole;
            ViewBag.IsDirectorRole = IsDirectorRole;

            var sourceFromList = GetSourceFrom(fromType).ToList();
            var statusSelectList = GetStatusList(status).ToList();
            sourceFromList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            statusSelectList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            ViewBag.SourceFrom = sourceFromList;
            ViewBag.Status = statusSelectList;

            IEnumerable<Department> _Departments = GetMyDepartmentList();
            SelectList Departments = new SelectList(_Departments, "DEPARTMENTID", "DEPARTMENTNAME", DepartmentId);
            var DepartmentList = Departments.ToList();
            if (_Departments.Count() > 1)
            {
                DepartmentList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Department = DepartmentList;

            IEnumerable<User> _Users=GetMyUser(DepartmentId);
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
            ViewBag.fromTypeParams = fromType;
            ViewBag.StatusParams = status;
            ViewBag.NextContractStartTimeParams = NextContractStartTime;
            ViewBag.NextContractEndTimeParams = NextContractEndTime;
            ViewBag.TelePhoneParams = TelePhone;
            ViewBag.MobilePhoneParams = MobilePhone;

            ViewBag.UpdateStartTimeParams = UpdateStartTime;
            ViewBag.UpdateEndTimeParams = UpdateEndTime;

            var customers = from item in db.Customer
                        where item.PoolStatus.Value==privateTag
                        select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (!string.IsNullOrEmpty(TelePhone))
            {
                customers = customers.Where(x => x.Tel.Contains(TelePhone));
            }
            if (!string.IsNullOrEmpty(MobilePhone))
            {
                customers = customers.Where(x => x.MobileTel.Contains(MobilePhone));
            }

            long depId = Convert.ToInt64(DepartmentId);
            if (depId > 0)
            {
                customers = customers.Where(c => c.DepartmentID == depId);
            }
            if (Owner == null)
            {
                long id=GetCurrentUser().USERID;
                customers = customers.Where(x => x.Owner == id);
            }
            if (Owner != null && Owner != "0")
            {
                long userId = Convert.ToInt64(Owner);
                customers = customers.Where(x => x.Owner == userId);
            }
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
            if (!string.IsNullOrEmpty(NextContractStartTime) && !string.IsNullOrEmpty(NextContractEndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(NextContractStartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(NextContractEndTime).ToString("yyyy-MM-dd 23:59:59"));
                customers = customers.Where(x => x.NextContactTime.Value >= st && x.NextContactTime <= et);
            }
            if (!string.IsNullOrEmpty(UpdateStartTime) && !string.IsNullOrEmpty(UpdateEndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(UpdateStartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(UpdateEndTime).ToString("yyyy-MM-dd 23:59:59"));
                customers = customers.Where(x => x.LastModify.Value >= st && x.LastModify <= et);
            }

            //过滤角色数据
            customers =Filter(customers);

            customers = customers.OrderByDescending(c => c.CreateDate);
            int pageNumber = (page ?? 1);
            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Create()
        {
            Tuple<bool, int, int> tuple = GetCustomersByOwner();
            if (tuple.Item1)
            {
                return Content(string.Format("<script >alert('你当前已经拥有客户数量：{0}个，超出系统允许的最大客户数量上限{1}。不允许添加新的客户！');javascript:window.opener=null;window.close();</script >", tuple.Item2, tuple.Item3), "text/html");
            }
            SelectList department = new SelectList(db.Department.ToList(), "departmentID", "departmentName", GetCurrentUser().DEPARTMENTID);
            ViewBag.SourceFrom = GetSourceFrom(string.Empty);
            ViewData["Status"] = GetStatusList(string.Empty);
            ViewData["Industry"]= GetIndustry(string.Empty);
            ViewBag.DepartmentList = department;
            return View();
        }

        [HttpPost]
        public ActionResult Create(Customer entity)
        {
            try
            {
                var cus = db.Customer.FirstOrDefault(x =>
                x.CorporationName.Trim() == entity.CorporationName.Trim());

               // var cus = db.Customer.FirstOrDefault(x =>
               //x.CorporationName.Trim() == entity.CorporationName.Trim()
               //|| (!string.IsNullOrEmpty(entity.Tel) && x.Tel.Trim() == entity.Tel.Trim())
               //|| (!string.IsNullOrEmpty(entity.MobileTel) && x.MobileTel.Trim() == entity.MobileTel.Trim()));

                if (cus != null)
                {
                    if (cus.PoolStatus == 2)
                    {
                        //return Json(new { success = false, message = "此客户在公共池中已存在！" });
                        return Content("<script >alert('此客户在公共池中已存在！');window.history.go( -1 ); </script >", "text/html");
                    }

                    if (cus.PoolStatus == 3)
                    {
                        //return Json(new { success = false, message = "此客户在废弃池中已存在！" });
                        return Content("<script >alert('此客户在废弃池中已存在！');window.history.go( -1 ); </script >", "text/html");
                    }

                    //return Json(new { success = false, message = string.Format("此客户已存在，所有者为{0}!", cus.User.USERNAME) });
                    return Content(string.Format("<script >alert('此客户已存在，所有者为{0}！');window.history.go( -1 ); </script >",cus.User.USERNAME), "text/html");
                }
                User user = GetCurrentUser();
                entity.Owner = user.USERID;
                entity.Creator = user.USERID;
                entity.DepartmentID = user.DEPARTMENTID;
                entity.PoolStatus = privateTag;
                entity.CreateDate = DateTime.Now;
                entity.LastModify = DateTime.Now;
                entity.BelongDateTime = DateTime.Now;
                db.Customer.Add(entity);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Content(string.Format("<script >alert('添加客户失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
            }
            return RedirectToAction("Create");
        }

        //[HttpPost]
        //public ActionResult Save(Customer entity)
        //{
        //    entity.Owner = GetCurrentUser().USERID;
        //    entity.CreateDate = DateTime.Now;
        //    entity.LastModify = DateTime.Now;
        //    db.Customer.Add(entity);
        //    db.SaveChanges();
        //    return RedirectToAction("Index");
        //}
        public ActionResult BackToList()
        {
            return RedirectToAction("Index");
        }

        public ActionResult Edit(long id)
        {
            Customer model = db.Customer.Find(id);
            SelectList department = new SelectList(db.Department.ToList(), "departmentID", "departmentName", model.DepartmentID);
            ViewBag.SourceFrom = GetSourceFrom(model.SourseFrom);
            //ViewData["Status"] = GetStatusList(model.Status);
            ViewData["Industry"] = GetIndustry(model.Industry);
            ViewBag.DepartmentList = department;
            return View(model);
        }
        [HttpPost]
        public ActionResult Edit(Customer entity)
        {
            try
            {

                //var cus = db.Customer.Where(x =>
                //x.CorporationName.Trim() == entity.CorporationName.Trim()
                //|| (!string.IsNullOrEmpty(entity.Tel) && x.Tel.Trim() == entity.Tel.Trim())
                //|| (!string.IsNullOrEmpty(entity.MobileTel) && x.MobileTel.Trim() == entity.MobileTel.Trim()));
                //if (cus != null && cus.Count() > 1)
                //{
                //    return Content(string.Format("<script >alert('编辑失败，错误信息:已经存在公司名称为'{0}'的公司或者手机电话信息出现重复！系统不允许公司相关信息重复！');window.history.go( -1 ); </script >", entity.CorporationName.Trim()), "text/html");
                //}

                entity.LastModify = DateTime.Now;
                db.Entry(entity).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Content(string.Format("<script >alert('编辑失败，错误信息:{0}');window.history.go( -1 ); </script >", ex.Message), "text/html");
            }
            return Content("<script >alert('编辑成功！');window.opener=null;window.close(); </script >", "text/html");
        }

        [HttpPost]
        public ActionResult Modify(Customer entity)
        {
            try
            {
                entity.LastModify = DateTime.Now;
                db.Entry(entity).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "编辑失败，原因:" + ex.Message });
            }
            return Json(new { success = true });
        }
        public ActionResult Details(long id)
        {
            Customer model = db.Customer.Find(id);
            return View(model);
        }

        public ActionResult Public(string CorporationName, string fromType, string status, string TelePhone, string MobilePhone,  int? page)
        {
            InitPageSize();
            ViewBag.IsRole = IsRole;
            ViewBag.IsDirectorRole = IsDirectorRole;

            var sourceFromList = GetSourceFrom(fromType).ToList();
            var statusSelectList = GetStatusList2(status).ToList();
            sourceFromList.Insert(0, new SelectListItem() { Text = "", Value = "" });
            statusSelectList.Insert(0, new SelectListItem() { Text = "", Value = "" });
            ViewBag.SourceFrom = sourceFromList;
            ViewBag.Status = statusSelectList;


            ViewBag.CorporationNameParams = CorporationName;
            ViewBag.fromTypeParams = fromType;
            ViewBag.StatusParams = status;
            ViewBag.TelePhoneParams = TelePhone;
            ViewBag.MobilePhoneParams = MobilePhone;

            var customers = from item in db.Customer
                            where item.PoolStatus.Value == publicTag
                            select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
            if (!string.IsNullOrEmpty(TelePhone))
            {
                customers = customers.Where(x => x.Tel.Contains(TelePhone));
            }
            if (!string.IsNullOrEmpty(MobilePhone))
            {
                customers = customers.Where(x => x.MobileTel.Contains(MobilePhone));
            }
            customers = customers.OrderByDescending(c => c.LastModify);
            int pageNumber = (page ?? 1);
            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult WastePool(string CorporationName, string fromType, string status, string TelePhone, string MobilePhone, int? page)
        {
            InitPageSize();
            ViewBag.IsRole = IsRole;
            ViewBag.IsDirectorRole = IsDirectorRole;

            var sourceFromList = GetSourceFrom(fromType).ToList();
            var statusSelectList = GetStatusList2(status).ToList();
            sourceFromList.Insert(0, new SelectListItem() { Text = "", Value = "" });
            statusSelectList.Insert(0, new SelectListItem() { Text = "", Value = "" });
            ViewBag.SourceFrom = sourceFromList;
            ViewBag.Status = statusSelectList;


            ViewBag.CorporationNameParams = CorporationName;
            ViewBag.fromTypeParams = fromType;
            ViewBag.StatusParams = status;
            ViewBag.TelePhoneParams = TelePhone;
            ViewBag.MobilePhoneParams = MobilePhone;

            var customers = from item in db.Customer
                            where item.PoolStatus.Value == obsoleteTag
                            select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
            if (!string.IsNullOrEmpty(TelePhone))
            {
                customers = customers.Where(x => x.Tel.Contains(TelePhone));
            }
            if (!string.IsNullOrEmpty(MobilePhone))
            {
                customers = customers.Where(x => x.MobileTel.Contains(MobilePhone));
            }
            customers = customers.OrderByDescending(c => c.LastModify);
            int pageNumber = (page ?? 1);
            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Delete(long id)
        {
            try
            {
                Customer model = db.Customer.Find(id);
                db.Customer.Remove(model);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败：" + ex.Message });
            }
            return Json(new { success = true });
        }

        public ActionResult Today(string CorporationName, string fromType, string Owner, string status, string NextContractStartTime, string NextContractEndTime, int? page)
        {
            InitPageSize();
            var sourceFromList = GetSourceFrom(fromType).ToList();
            var statusSelectList = GetStatusList(status).ToList();
            sourceFromList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            statusSelectList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            ViewBag.SourceFrom = sourceFromList;
            ViewBag.Status = statusSelectList;


            IEnumerable<User> _Users = GetMyUser();
            SelectList Users = new SelectList(_Users, "USERID", "USERNAME", Owner);
            var usersList = Users.ToList();
            if (_Users.Count() > 1)
            {
                usersList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Owner = usersList;

            ViewBag.CorporationNameParams = CorporationName;
            ViewBag.UserIdParams = Owner;
            ViewBag.fromTypeParams = fromType;
            ViewBag.StatusParams = status;
            ViewBag.NextContractStartTimeParams = NextContractStartTime;
            ViewBag.NextContractEndTimeParams = NextContractEndTime;

            DateTime beginDate;
            DateTime endDate;
            DateTime nowDate = DateTime.Now;
            beginDate = nowDate.Date;
            endDate = nowDate.Date.AddDays(1).AddSeconds(-1);
            var customers = from item in db.Customer
                            where item.PoolStatus.Value == privateTag && item.CreateDate.Value >= beginDate && item.CreateDate.Value <= endDate
                            select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (Owner == null)
            {
                long id = GetCurrentUser().USERID;
                customers = customers.Where(x => x.Owner == id);
            }
            if (Owner != null && Owner != "0")
            {
                long userId = Convert.ToInt64(Owner);
                customers = customers.Where(x => x.Owner == userId);
            }
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
            if (!string.IsNullOrEmpty(NextContractStartTime) && !string.IsNullOrEmpty(NextContractEndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(NextContractStartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(NextContractEndTime).ToString("yyyy-MM-dd 23:59:59"));
                customers = customers.Where(x => x.NextContactTime.Value >= st && x.NextContactTime <= et);
            }

            //过滤角色数据
            customers = Filter(customers);

            customers = customers.OrderByDescending(c => c.CreateDate);
            int pageNumber = (page ?? 1);
            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Week(string CorporationName, string fromType, string Owner, string status, string NextContractStartTime, string NextContractEndTime, int? page)
        {
            InitPageSize();
            var sourceFromList = GetSourceFrom(fromType).ToList();
            var statusSelectList = GetStatusList(status).ToList();
            sourceFromList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            statusSelectList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            ViewBag.SourceFrom = sourceFromList;
            ViewBag.Status = statusSelectList;


            IEnumerable<User> _Users = GetMyUser();
            SelectList Users = new SelectList(_Users, "USERID", "USERNAME", Owner);
            var usersList = Users.ToList();
            if (_Users.Count() > 1)
            {
                usersList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Owner = usersList;

            ViewBag.CorporationNameParams = CorporationName;
            ViewBag.UserIdParams = Owner;
            ViewBag.fromTypeParams = fromType;
            ViewBag.StatusParams = status;
            ViewBag.NextContractStartTimeParams = NextContractStartTime;
            ViewBag.NextContractEndTimeParams = NextContractEndTime;

            DateTime beginDate;
            DateTime endDate;
            DateTime nowDate = DateTime.Now;
            beginDate = nowDate.Date.AddDays(1 - Convert.ToInt32(nowDate.DayOfWeek.ToString("d")));
            endDate = beginDate.Date.AddDays(7).AddSeconds(-1);

            var customers = from item in db.Customer
                            where item.PoolStatus.Value == privateTag && item.CreateDate.Value >= beginDate && item.CreateDate.Value <= endDate
                            select item;

            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (Owner == null)
            {
                long id = GetCurrentUser().USERID;
                customers = customers.Where(x => x.Owner == id);
            }
            if (Owner != null && Owner != "0")
            {
                long userId = Convert.ToInt64(Owner);
                customers = customers.Where(x => x.Owner == userId);
            }
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
            if (!string.IsNullOrEmpty(NextContractStartTime) && !string.IsNullOrEmpty(NextContractEndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(NextContractStartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(NextContractEndTime).ToString("yyyy-MM-dd 23:59:59"));
                customers = customers.Where(x => x.NextContactTime.Value >= st && x.NextContactTime <= et);
            }

            //过滤角色数据
            customers = Filter(customers);

            customers = customers.OrderByDescending(c => c.CreateDate);
            int pageNumber = (page ?? 1);
            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Month(string CorporationName, string fromType, string Owner, string status, string NextContractStartTime, string NextContractEndTime, int? page)
        {
            InitPageSize();
            var sourceFromList = GetSourceFrom(fromType).ToList();
            var statusSelectList = GetStatusList(status).ToList();
            sourceFromList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            statusSelectList.Insert(0, new SelectListItem() { Text = "全部", Value = "" });
            ViewBag.SourceFrom = sourceFromList;
            ViewBag.Status = statusSelectList;


            IEnumerable<User> _Users = GetMyUser();
            SelectList Users = new SelectList(_Users, "USERID", "USERNAME", Owner);
            var usersList = Users.ToList();
            if (_Users.Count() > 1)
            {
                usersList.Insert(0, new SelectListItem() { Text = "全部", Value = "0" });
            }
            ViewBag.Owner = usersList;

            ViewBag.CorporationNameParams = CorporationName;
            ViewBag.UserIdParams = Owner;
            ViewBag.fromTypeParams = fromType;
            ViewBag.StatusParams = status;
            ViewBag.NextContractStartTimeParams = NextContractStartTime;
            ViewBag.NextContractEndTimeParams = NextContractEndTime;

            DateTime beginDate;
            DateTime endDate;
            DateTime nowDate = DateTime.Now;
            beginDate = nowDate.Date.AddDays(1 - nowDate.Day);
            endDate = beginDate.AddMonths(1).AddSeconds(-1);

            var customers = from item in db.Customer
                            where item.PoolStatus.Value == privateTag && item.CreateDate.Value >= beginDate && item.CreateDate.Value <= endDate
                            select item;

            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (Owner == null)
            {
                long id = GetCurrentUser().USERID;
                customers = customers.Where(x => x.Owner == id);
            }
            //else
            //{
            //    long userId = Convert.ToInt64(Owner);
            //    customers = customers.Where(x => x.Owner == userId);
            //}
            if (Owner != null && Owner != "0")
            {
                long userId = Convert.ToInt64(Owner);
                customers = customers.Where(x => x.Owner == userId);
            }
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
            if (!string.IsNullOrEmpty(NextContractStartTime) && !string.IsNullOrEmpty(NextContractEndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(NextContractStartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(NextContractEndTime).ToString("yyyy-MM-dd 23:59:59"));
                customers = customers.Where(x => x.NextContactTime.Value >= st && x.NextContactTime <= et);
            }

            //过滤角色数据
            customers=Filter(customers);

            customers = customers.OrderByDescending(c => c.CreateDate);
            int pageNumber = (page ?? 1);
            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult BatchOperation()
        {
            //ViewBag.CustomerIds = CustomerIds;

            User currentUser = GetCurrentUser();
            
            var statusSelectList = GetStatusList(string.Empty).ToList();
            statusSelectList.Insert(0, new SelectListItem() { Text = "", Value = "" });
            ViewBag.Status = statusSelectList;

            SelectList Users = new SelectList(GetMyUser(), "USERID", "USERNAME", currentUser.USERID);
            var usersList = Users.ToList();
            usersList.Insert(0, new SelectListItem() { Text = "", Value = "0" });

            ViewBag.Owner = usersList;
            ViewBag.NextContractTimeParams = null;
            return View();
        }

        /// <summary>
        /// 批量更新操作，可以更新所有者，状态，下次联系时间
        /// </summary>
        /// <param name="CustomerIds"></param>
        /// <param name="Owner"></param>
        /// <param name="Status"></param>
        /// <param name="NextContractTime"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult BatchOperation(Operator @operator)
        {
            try
            {
                Task.Run(() => {

                    using (Iso58Entities contenxt = new Iso58Entities())
                    {

                        long DepartmentID = 0;
                        if (@operator.Owner.HasValue && @operator.Owner.Value > 0)
                        {
                            //所有者所在部门
                            DepartmentID = contenxt.User.Where(c => c.USERID == @operator.Owner.Value).SingleOrDefault().DEPARTMENTID;
                        }
                        string[] ids = @operator.CustomerIds.Split(new char[] { ',' });
                        foreach (var id in ids)
                        {
                            long cid = Convert.ToInt64(id);
                            Customer model = contenxt.Customer.Where(c => c.CustomerID == cid).SingleOrDefault();
                            model.LastModify = DateTime.Now;
                            //更新所有者
                            if (@operator.Owner.HasValue && @operator.Owner.Value > 0)
                            {
                                model.Owner = @operator.Owner.Value;
                                if (DepartmentID > 0)
                                {
                                    //所有者所在部门
                                    model.DepartmentID = DepartmentID;
                                }
                                //最新入库时间
                                model.BelongDateTime = DateTime.Now;
                                model.PoolStatus = privateTag;
                            }
                            if (!string.IsNullOrEmpty(@operator.Status))
                            {
                                model.Status = @operator.Status;
                            }
                            if (!string.IsNullOrEmpty(@operator.NextContractTime))
                            {
                                model.NextContactTime = Convert.ToDateTime(@operator.NextContractTime);
                            }
                            model.LastModify = DateTime.Now;
                            contenxt.Entry(model).State = System.Data.Entity.EntityState.Modified;
                            contenxt.SaveChanges();
                        }
                    }
                    
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败："+ex.Message });
            }
            return Json(new { success = true });
        }
        public ActionResult Move(string Ids, short PoolStatus)
        {
            try
            {
                string[] ids = Ids.Split(new char[] { ',' });
                if (PoolStatus == 1)
                {
                    Tuple<bool, int, int> tuple = GetCustomersByOwner();
                    if (tuple.Item1)
                    {
                        return Json(new { success = false, message = string.Format("你当前已经拥有客户数量：{0}个，超出系统允许的最大客户数量上限{1}。不允许移入新的客户!", tuple.Item2, tuple.Item3)});
                    }
                }
                
                foreach (var id in ids)
                {
                    long cid = Convert.ToInt64(id);
                    Customer model = db.Customer.Where(c => c.CustomerID == cid).SingleOrDefault();
                    model.LastModify = DateTime.Now;
                    model.PoolStatus = PoolStatus;
                    if (PoolStatus == 1)
                    {
                        model.Owner = GetCurrentUser().USERID;
                        model.DepartmentID = GetCurrentUser().DEPARTMENTID;
                        //最新入库时间
                        model.BelongDateTime = DateTime.Now;
                    }
                    db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败：" + ex.Message });
            }
            return Json(new { success = true });
        }

        public ActionResult ClearInfo(string Ids)
        {
            try
            {
                string[] ids = Ids.Split(new char[] { ',' });
                foreach (var id in ids)
                {
                    long cid = Convert.ToInt64(id);
                    Customer model = db.Customer.Where(c => c.CustomerID == cid).SingleOrDefault();
                    model.FirstContactDate = null;
                    model.FirstRemark = null;
                    model.SecondContactDate = null;
                    model.SecondRemark = null;
                    model.ThirdContactDate = null;
                    model.ThirdRemark = null;
                    model.FourthContactDate = null;
                    model.FourthRemark = null;
                    model.FifthContactDate = null;
                    model.FifthRemark = null;
                    model.SixthContactDate = null;
                    model.SixthRemark = null;
                    model.SeventhContactDate = null;
                    model.SeventhRemark = null;
                    model.EighthContactDate = null;
                    model.EighthRemark = null;
                    model.NinthContactDate = null;
                    model.NinthRemark = null;
                    model.TenthContactDate = null;
                    model.TenthRemark = null;

                    model.ElevenContactDate = null;
                    model.ElevenRemark = null;
                    model.twelveContactDate = null;
                    model.twelveRemark = null;
                    model.thirteenContactDate = null;
                    model.thirteenRemark = null;
                    model.fourteenContactDate = null;
                    model.fourteenRemark = null;
                    model.fifteenContactDate = null;
                    model.fifteenRemark = null;
                    model.sixteenContactDate = null;
                    model.sixteenRemark = null;
                    model.seventeenContactDate = null;
                    model.seventeenRemark = null;
                    model.eighteenContactDate = null;
                    model.eighteenRemark = null;
                    model.nineteenContactDate = null;
                    model.nineteenRemark = null;
                    model.twentyContactDate = null;
                    model.twentyRemark = null;

                    model.LastModify = DateTime.Now;
                    db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败：" + ex.Message });
            }
            return Json(new { success = true });
        }


        public ActionResult CheckCorporationName(string CorporationName)
        {
            CorporationName = CorporationName.Trim();
            Customer customer = db.Customer.Where(c => c.CorporationName.Trim() == CorporationName.Trim()).FirstOrDefault();
            if (customer == null)
            {
                return Json(new { success = true });
            }

            if (customer.PoolStatus == 2)
            {
                return Json(new { success = false, message = "此客户在公共池中已存在！" , OperationCode =2  });
            }

            if (customer.PoolStatus == 3)
            {
                return Json(new { success = false, message = "此客户在废弃池中已存在！", OperationCode = 3 });
            }
            //王利的用户
            if (customer.Owner==1)
            {
                return Json(new { success = false, message = string.Format("此客户已存在，所有者为{0}!,你可以拥有！", customer.User.USERNAME), OperationCode =4 });
            }

            return Json(new { success = false, message = string.Format("此客户已存在，所有者为{0}!", customer.User.USERNAME), OperationCode = 1});
        }

        /// <summary>
        /// 抢占客户，归入自己客户列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult GrabCustomer(string CorporationName)
        {
            try
            {
                Tuple<bool, int, int> tuple = GetCustomersByOwner();
                if (tuple.Item1)
                {
                    return Json(new { success = false, message = string.Format("你当前已经拥有客户数量：{0}个，超出系统允许的最大客户数量上限{1}。不允许抢占新的客户!", tuple.Item2, tuple.Item3) });
                }
                CorporationName = CorporationName.Trim();
                Customer model = db.Customer.Where(c => c.CorporationName == CorporationName).SingleOrDefault();
                model.LastModify = DateTime.Now;
                model.Owner = GetCurrentUser().USERID;
                model.DepartmentID = GetCurrentUser().DEPARTMENTID;
                model.PoolStatus = privateTag;
                //最新入库时间
                model.BelongDateTime = DateTime.Now;
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, CustomerId=model.CustomerID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "此客户抢占失败，原因:"+ex.Message });
            }
        }

        [HttpPost]
        public ActionResult BatchDelete(string CustomerIds)
        {
            try
            {
                string[] ids = CustomerIds.Split(new char[] { ',' });
                foreach (var id in ids)
                {
                    long cid = Convert.ToInt64(id);
                    Customer model = db.Customer.Where(c => c.CustomerID == cid).SingleOrDefault();
                    db.Customer.Remove(model);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败：" + ex.Message });
            }
            return Json(new { success = true });
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
               "普4类",
               "重4类",
               "5类",
               "6类",
               "9类",
               "已联系"
               //,
               //"错误信息",
               //"不是收费会员",
               //"无合作意向"
            };
            SelectList statusList = null;
            if (selectedValue != null && selectedValue.ToString() != string.Empty)
            {
                statusList = new SelectList(lst, selectedValue);
            }
            else
            {
                statusList = new SelectList(lst);
            }
            return statusList;
        }
        private SelectList GetStatusList2(object selectedValue)
        {
            List<string> lst = new List<string>()
            {
               "0类",
               "1类",
               "2类",
               "普3类",
               "重3类",
               "普4类",
               "重4类",
               "5类",
               "6类",
               "9类",
               "已联系",
                "错误信息",
                "不是收费会员",
                "无合作意向"
            };
            SelectList statusList = null;
            if (selectedValue != null && selectedValue.ToString() != string.Empty)
            {
                statusList = new SelectList(lst, selectedValue);
            }
            else
            {
                statusList = new SelectList(lst);
            }
            return statusList;
        }
        private SelectList GetSourceFrom(object selectedValue)
        {
            SelectList sourceFrom = new SelectList(new List<string>()
            {
                "百度推广",
                "百度优化",
                "360推广",
                "搜搜推广",
                "搜狗推广",
                "有道推广",
                "慧聪标王",
                "阿里诚信通会员",
                "58同城",
                "慧聪免费会员",
                "慧聪买卖通会员",
                "其他",
                "广告",
                "阿里转化",
                "阿里慧聪",
                "ICP备案"
            }, selectedValue);
            return sourceFrom;
        }

        private SelectList GetIndustry(object selectedValue)
        {
            SelectList industry = new SelectList(new List<string>()
                {
                    "农业",
                    "服装",
                    "教育",
                    "电子",
                    "食品与饮料",
                    "能源"
                },selectedValue);
                return industry;
        }

        private Tuple<bool,int,int> GetCustomersByOwner()
        {
            int EmployeeLimitCustomer = Convert.ToInt32(ConfigurationManager.AppSettings["EmployeeLimit-Customer"]);
            int ManagerLimitCustomer = Convert.ToInt32(ConfigurationManager.AppSettings["ManagerLimit-Customer"]);

            int limit = 0;
            User currentUser = GetCurrentUser();
            var items=db.Customer.Where(c => c.Owner == currentUser.USERID && c.PoolStatus.Value == privateTag);

            bool isGreater = false;
            if(currentUser.Department.DEPARTMENTNAME.Equals("客服部"))
            {
                limit = 20000;
                isGreater = items.Count() > limit ? true : false;
                return new Tuple<bool, int, int>(isGreater, items.Count(), limit);
            }
            if (currentUser.Role.ROLENAME == "销售经理")
            {
                limit = ManagerLimitCustomer;
                isGreater = items.Count() > limit ? true : false;
            }
            else if (currentUser.Role.ROLENAME == "高级销售经理-群总" || currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
            {
                limit = ManagerLimitCustomer * 30;
                isGreater = items.Count() > limit ? true : false;
            }
            else
            {
                limit = EmployeeLimitCustomer;
                //仅能看自己数据
                isGreater = items.Count() > limit ? true : false;
            }
            return new Tuple<bool, int,int>(isGreater, items.Count(),limit);
        }

        /// <summary>
        /// 根据权限获取数据
        /// </summary>
        /// <param name="_items"></param>
        /// <returns></returns>
        private IQueryable<Customer> Filter(IEnumerable<Customer> items)
        {
            User currentUser = GetCurrentUser();
            if (currentUser.Role.ROLENAME == "销售经理")
            {
                //仅能看部门内数据
                items = items.Where(y => y.DepartmentID == currentUser.DEPARTMENTID);
            }
            else if (currentUser.Role.ROLENAME == "高级销售经理-群总")
            {
                //看权限设置的部门数据

                var authDeps = from auth in db.Authority
                               where auth.UserId == currentUser.USERID
                               select auth.DepartmentId;
                if (authDeps.Count() > 0)
                {
                    items = items.Where(d => authDeps.Contains(d.DepartmentID));
                }
                else
                {
                    items = items.Where(y => y.DepartmentID == currentUser.DEPARTMENTID);
                }
            }
            else if (currentUser.Role.ROLENAME == "销售总监" || currentUser.Role.ROLENAME == "高级管理员")
            {
                //全部数据
            }
            else
            {
                //仅能看自己数据
                items = items.Where(y => y.Owner == currentUser.USERID);
            }
            return items.AsQueryable();
        }
        
        [HttpGet]
        public JsonResult GetEmployees(long departmentId)
        {
            var items = from item in db.User.Where(u=>u.USERSTATE=="1") select item;
            if (departmentId > 0)
            {
                items = items.Where(item => item.DEPARTMENTID== departmentId);
            }
            else
            {
                long currentUid = GetCurrentUser().USERID;
                var authDeps = from auth in db.Authority
                               where auth.UserId == currentUid
                               select auth.DepartmentId;
                if (authDeps.Count() > 0)
                {
                    items = items.Where(d => authDeps.Contains(d.DEPARTMENTID));
                }
            }
            var values=from item in items
                  select new { UserId = item.USERID, UserName = item.USERNAME };
            return Json(values.ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetMyCustomers(string CorporationName)
        {
            User currentUser = GetCurrentUser();
            var items = from item in db.Customer select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                items = items.Where(item => item.Owner == currentUser.USERID && item.CorporationName.Contains(CorporationName));
            }
            var values = from item in items
                         select new { CustomerID=item.CustomerID, CorporationName=item.CorporationName };
            return Json(values.ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public FileResult Export(string CorporationName, string fromType, string DepartmentId, string Owner, string status, string NextContractStartTime, string NextContractEndTime, string TelePhone, string MobilePhone, string UpdateStartTime, string UpdateEndTime, int? page)
        {

            var customers = from item in db.Customer
                            where item.PoolStatus.Value == privateTag
                            select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (!string.IsNullOrEmpty(TelePhone))
            {
                customers = customers.Where(x => x.Tel.Contains(TelePhone));
            }
            if (!string.IsNullOrEmpty(MobilePhone))
            {
                customers = customers.Where(x => x.MobileTel.Contains(MobilePhone));
            }

            long depId = Convert.ToInt64(DepartmentId);
            if (depId > 0)
            {
                customers = customers.Where(c => c.DepartmentID == depId);
            }
            //if (Owner == null)
            //{
            //    long id = GetCurrentUser().USERID;
            //    customers = customers.Where(x => x.Owner == id);
            //}
            if (Owner != null && Owner != "0")
            {
                long userId = Convert.ToInt64(Owner);
                customers = customers.Where(x => x.Owner == userId);
            }
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
            if (!string.IsNullOrEmpty(NextContractStartTime) && !string.IsNullOrEmpty(NextContractEndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(NextContractStartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(NextContractEndTime).ToString("yyyy-MM-dd 23:59:59"));
                customers = customers.Where(x => x.NextContactTime.Value >= st && x.NextContactTime <= et);
            }
            if (!string.IsNullOrEmpty(UpdateStartTime) && !string.IsNullOrEmpty(UpdateEndTime))
            {
                DateTime st = DateTime.Parse(DateTime.Parse(UpdateStartTime).ToString("yyyy-MM-dd 00:00:00"));
                DateTime et = DateTime.Parse(DateTime.Parse(UpdateEndTime).ToString("yyyy-MM-dd 23:59:59"));
                customers = customers.Where(x => x.LastModify.Value >= st && x.LastModify <= et);
            }

            //过滤角色数据
            customers = Filter(customers);

            var list = customers.ToList();

            //创建Excel文件的对象
            NPOI.HSSF.UserModel.HSSFWorkbook book = new NPOI.HSSF.UserModel.HSSFWorkbook();
            //添加一个sheet
            NPOI.SS.UserModel.ISheet sheet1 = book.CreateSheet("Sheet1");

            /*
             * SELECT top 100
      [CorporationName] as 公司名称
	  ,[CustomerName] as 联系人
      ,[Tel] as 电话
      ,[Fax] as 传真
	  ,[Email] as 邮箱
      ,[City]as 城市
      ,[MobileTel] as 手机
      ,[SourseFrom] as 来源
	   ,[Status] as 状态
      ,[Industry] as 行业
	   ,[Province] as 省份
      ,[RegisteredCapital] as 城市
      ,[Address] as 地址
      ,[WebSite] as 网址
      ,[DepartmentID] as 归属销售部门
      ,[CustomerType] as 客户类型
      ,[NextContactTime] as 下次联系时间
      ,[LastModify] as 最近修改时间
	  ,[CreateDate] as 创建时间
	  ,[Creator] as 创建人
	   ,[Owner] as 客户归属
	  ,[BelongDateTime] 客户归属变更时间
      ,replace(replace([Description],char(10),' '),char(13),' ') as 描述
	   ,[WebSiteRemark] as 备注
  FROM [dbo].[Customer] 
             */

            //给sheet1添加第一行的头部标题
            NPOI.SS.UserModel.IRow row1 = sheet1.CreateRow(0);
            row1.CreateCell(0).SetCellValue("ID");
            row1.CreateCell(1).SetCellValue("公司名称");
            row1.CreateCell(2).SetCellValue("联系人");
            row1.CreateCell(3).SetCellValue("电话");
            row1.CreateCell(4).SetCellValue("传真");
            row1.CreateCell(5).SetCellValue("邮箱");
            row1.CreateCell(6).SetCellValue("手机");
            row1.CreateCell(7).SetCellValue("来源");
            row1.CreateCell(8).SetCellValue("状态");
            row1.CreateCell(9).SetCellValue("行业");
            row1.CreateCell(10).SetCellValue("省份");
            row1.CreateCell(11).SetCellValue("城市");
            row1.CreateCell(12).SetCellValue("地址");
            row1.CreateCell(13).SetCellValue("归属销售部门");
            row1.CreateCell(14).SetCellValue("网址");
            row1.CreateCell(15).SetCellValue("客户类型");
            row1.CreateCell(16).SetCellValue("下次联系时间");
            row1.CreateCell(17).SetCellValue("最近修改时间");
            row1.CreateCell(18).SetCellValue("创建时间");
            row1.CreateCell(19).SetCellValue("创建人");
            row1.CreateCell(20).SetCellValue("客户归属");
            row1.CreateCell(21).SetCellValue("客户归属变更时间");
            row1.CreateCell(22).SetCellValue("描述");
            row1.CreateCell(23).SetCellValue("备注");

            row1.CreateCell(24).SetCellValue("第1次联系时间");
            row1.CreateCell(25).SetCellValue("联系内容");
            row1.CreateCell(26).SetCellValue("第2次联系时间");
            row1.CreateCell(27).SetCellValue("联系内容");
            row1.CreateCell(28).SetCellValue("第3次联系时间");
            row1.CreateCell(29).SetCellValue("联系内容");
            row1.CreateCell(30).SetCellValue("第4次联系时间");
            row1.CreateCell(31).SetCellValue("联系内容");
            row1.CreateCell(32).SetCellValue("第5次联系时间");
            row1.CreateCell(33).SetCellValue("联系内容");
            row1.CreateCell(34).SetCellValue("第6次联系时间");
            row1.CreateCell(35).SetCellValue("联系内容");
            row1.CreateCell(36).SetCellValue("第7次联系时间");
            row1.CreateCell(37).SetCellValue("联系内容");
            row1.CreateCell(38).SetCellValue("第8次联系时间");
            row1.CreateCell(39).SetCellValue("联系内容");
            row1.CreateCell(40).SetCellValue("第9次联系时间");
            row1.CreateCell(41).SetCellValue("联系内容");
            row1.CreateCell(42).SetCellValue("第10次联系时间");
            row1.CreateCell(43).SetCellValue("联系内容");

            row1.CreateCell(44).SetCellValue("第11次联系时间");
            row1.CreateCell(45).SetCellValue("联系内容");
            row1.CreateCell(46).SetCellValue("第12次联系时间");
            row1.CreateCell(47).SetCellValue("联系内容");
            row1.CreateCell(48).SetCellValue("第13次联系时间");
            row1.CreateCell(49).SetCellValue("联系内容");
            row1.CreateCell(50).SetCellValue("第14次联系时间");
            row1.CreateCell(51).SetCellValue("联系内容");
            row1.CreateCell(52).SetCellValue("第15次联系时间");
            row1.CreateCell(53).SetCellValue("联系内容");
            row1.CreateCell(54).SetCellValue("第16次联系时间");
            row1.CreateCell(55).SetCellValue("联系内容");
            row1.CreateCell(56).SetCellValue("第17次联系时间");
            row1.CreateCell(57).SetCellValue("联系内容");
            row1.CreateCell(58).SetCellValue("第18次联系时间");
            row1.CreateCell(59).SetCellValue("联系内容");
            row1.CreateCell(60).SetCellValue("第19次联系时间");
            row1.CreateCell(61).SetCellValue("联系内容");
            row1.CreateCell(62).SetCellValue("第20次联系时间");
            row1.CreateCell(63).SetCellValue("联系内容");

            //将数据逐步写入sheet1各个行
            for (int i = 0; i < list.Count(); i++)
            {
                NPOI.SS.UserModel.IRow rowtemp = sheet1.CreateRow(i + 1);
                rowtemp.CreateCell(0).SetCellValue(list[i].CustomerID);
                rowtemp.CreateCell(1).SetCellValue(list[i].CorporationName);
                rowtemp.CreateCell(2).SetCellValue(list[i].CustomerName);
                rowtemp.CreateCell(3).SetCellValue(list[i].Tel);
                rowtemp.CreateCell(4).SetCellValue(list[i].Fax);
                rowtemp.CreateCell(5).SetCellValue(list[i].Email);
                rowtemp.CreateCell(6).SetCellValue(list[i].MobileTel);
                rowtemp.CreateCell(7).SetCellValue(list[i].SourseFrom);
                rowtemp.CreateCell(8).SetCellValue(list[i].Status);
                rowtemp.CreateCell(9).SetCellValue(list[i].Industry);
                rowtemp.CreateCell(10).SetCellValue(list[i].Province);
                rowtemp.CreateCell(11).SetCellValue(list[i].City);
                rowtemp.CreateCell(12).SetCellValue(list[i].Address);
                rowtemp.CreateCell(13).SetCellValue(list[i].Department.DEPARTMENTNAME);
                rowtemp.CreateCell(14).SetCellValue(list[i].WebSite);
                rowtemp.CreateCell(15).SetCellValue(list[i].CustomerType);
                rowtemp.CreateCell(16).SetCellValue(list[i].NextContactTime.HasValue?list[i].NextContactTime.Value:DateTime.MinValue);
                rowtemp.CreateCell(17).SetCellValue(list[i].LastModify.HasValue?list[i].LastModify.Value:DateTime.MinValue);
                rowtemp.CreateCell(18).SetCellValue(list[i].CreateDate.HasValue?list[i].CreateDate.Value:DateTime.MinValue);
                rowtemp.CreateCell(19).SetCellValue(list[i].Creator.HasValue?list[i].Creator.Value:0);
                rowtemp.CreateCell(20).SetCellValue(list[i].Owner);
                rowtemp.CreateCell(21).SetCellValue(list[i].BelongDateTime.HasValue? list[i].BelongDateTime.Value:DateTime.MinValue);
                rowtemp.CreateCell(22).SetCellValue(list[i].Description);
                rowtemp.CreateCell(23).SetCellValue(list[i].WebSiteRemark);

                rowtemp.CreateCell(24).SetCellValue(list[i].FirstContactDate.HasValue ? list[i].FirstContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(25).SetCellValue(list[i].FirstRemark);

                rowtemp.CreateCell(26).SetCellValue(list[i].SecondContactDate.HasValue ? list[i].SecondContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(27).SetCellValue(list[i].SecondRemark);

                rowtemp.CreateCell(28).SetCellValue(list[i].ThirdContactDate.HasValue ? list[i].ThirdContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(29).SetCellValue(list[i].ThirdRemark);

                rowtemp.CreateCell(30).SetCellValue(list[i].FourthContactDate.HasValue ? list[i].FourthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(31).SetCellValue(list[i].FourthRemark);

                rowtemp.CreateCell(32).SetCellValue(list[i].FifthContactDate.HasValue ? list[i].FifthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(33).SetCellValue(list[i].FifthRemark);

                rowtemp.CreateCell(34).SetCellValue(list[i].SixthContactDate.HasValue ? list[i].SixthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(35).SetCellValue(list[i].SixthRemark);

                rowtemp.CreateCell(36).SetCellValue(list[i].SeventhContactDate.HasValue ? list[i].SeventhContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(37).SetCellValue(list[i].SeventhRemark);

                rowtemp.CreateCell(38).SetCellValue(list[i].EighthContactDate.HasValue ? list[i].EighthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(39).SetCellValue(list[i].EighthRemark);

                rowtemp.CreateCell(40).SetCellValue(list[i].NinthContactDate.HasValue ? list[i].NinthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(41).SetCellValue(list[i].NinthRemark);

                rowtemp.CreateCell(42).SetCellValue(list[i].TenthContactDate.HasValue ? list[i].TenthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(43).SetCellValue(list[i].TenthRemark);

                rowtemp.CreateCell(44).SetCellValue(list[i].ElevenContactDate.HasValue ? list[i].ElevenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(45).SetCellValue(list[i].ElevenRemark);

                rowtemp.CreateCell(46).SetCellValue(list[i].twelveContactDate.HasValue ? list[i].twelveContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(47).SetCellValue(list[i].twelveRemark);

                rowtemp.CreateCell(48).SetCellValue(list[i].thirteenContactDate.HasValue ? list[i].thirteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(49).SetCellValue(list[i].thirteenRemark);

                rowtemp.CreateCell(50).SetCellValue(list[i].fourteenContactDate.HasValue ? list[i].fourteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(51).SetCellValue(list[i].fourteenRemark);

                rowtemp.CreateCell(52).SetCellValue(list[i].fifteenContactDate.HasValue ? list[i].fifteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(53).SetCellValue(list[i].fifteenRemark);

                rowtemp.CreateCell(54).SetCellValue(list[i].sixteenContactDate.HasValue ? list[i].sixteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(55).SetCellValue(list[i].sixteenRemark);

                rowtemp.CreateCell(56).SetCellValue(list[i].seventeenContactDate.HasValue ? list[i].seventeenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(57).SetCellValue(list[i].seventeenRemark);

                rowtemp.CreateCell(58).SetCellValue(list[i].eighteenContactDate.HasValue ? list[i].eighteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(59).SetCellValue(list[i].eighteenRemark);

                rowtemp.CreateCell(60).SetCellValue(list[i].nineteenContactDate.HasValue ? list[i].nineteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(61).SetCellValue(list[i].nineteenRemark);

                rowtemp.CreateCell(62).SetCellValue(list[i].twentyContactDate.HasValue ? list[i].twentyContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(63).SetCellValue(list[i].twentyRemark);
            }
            // 写入到客户端 
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            book.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms, "application/vnd.ms-excel", DateTime.Today.ToLongDateString()+"_我的客户信息.xls");
        }

        [HttpGet]
        public FileResult Export_Public(string CorporationName, string fromType, string status, string TelePhone, string MobilePhone, int? page)
        {

            var customers = from item in db.Customer
                            where item.PoolStatus.Value == publicTag
                            select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (!string.IsNullOrEmpty(TelePhone))
            {
                customers = customers.Where(x => x.Tel.Contains(TelePhone));
            }
            if (!string.IsNullOrEmpty(MobilePhone))
            {
                customers = customers.Where(x => x.MobileTel.Contains(MobilePhone));
            }

          
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
          
            //过滤角色数据
            customers = Filter(customers);

            var list = customers.ToList();

            //创建Excel文件的对象
            NPOI.HSSF.UserModel.HSSFWorkbook book = new NPOI.HSSF.UserModel.HSSFWorkbook();
            //添加一个sheet
            NPOI.SS.UserModel.ISheet sheet1 = book.CreateSheet("Sheet1");

            /*
             * SELECT top 100
      [CorporationName] as 公司名称
	  ,[CustomerName] as 联系人
      ,[Tel] as 电话
      ,[Fax] as 传真
	  ,[Email] as 邮箱
      ,[City]as 城市
      ,[MobileTel] as 手机
      ,[SourseFrom] as 来源
	   ,[Status] as 状态
      ,[Industry] as 行业
	   ,[Province] as 省份
      ,[RegisteredCapital] as 城市
      ,[Address] as 地址
      ,[WebSite] as 网址
      ,[DepartmentID] as 归属销售部门
      ,[CustomerType] as 客户类型
      ,[NextContactTime] as 下次联系时间
      ,[LastModify] as 最近修改时间
	  ,[CreateDate] as 创建时间
	  ,[Creator] as 创建人
	   ,[Owner] as 客户归属
	  ,[BelongDateTime] 客户归属变更时间
      ,replace(replace([Description],char(10),' '),char(13),' ') as 描述
	   ,[WebSiteRemark] as 备注
  FROM [dbo].[Customer] 
             */

            //给sheet1添加第一行的头部标题
            NPOI.SS.UserModel.IRow row1 = sheet1.CreateRow(0);
            row1.CreateCell(0).SetCellValue("ID");
            row1.CreateCell(1).SetCellValue("公司名称");
            row1.CreateCell(2).SetCellValue("联系人");
            row1.CreateCell(3).SetCellValue("电话");
            row1.CreateCell(4).SetCellValue("传真");
            row1.CreateCell(5).SetCellValue("邮箱");
            row1.CreateCell(6).SetCellValue("手机");
            row1.CreateCell(7).SetCellValue("来源");
            row1.CreateCell(8).SetCellValue("状态");
            row1.CreateCell(9).SetCellValue("行业");
            row1.CreateCell(10).SetCellValue("省份");
            row1.CreateCell(11).SetCellValue("城市");
            row1.CreateCell(12).SetCellValue("地址");
            row1.CreateCell(13).SetCellValue("归属销售部门");
            row1.CreateCell(14).SetCellValue("网址");
            row1.CreateCell(15).SetCellValue("客户类型");
            row1.CreateCell(16).SetCellValue("下次联系时间");
            row1.CreateCell(17).SetCellValue("最近修改时间");
            row1.CreateCell(18).SetCellValue("创建时间");
            row1.CreateCell(19).SetCellValue("创建人");
            row1.CreateCell(20).SetCellValue("客户归属");
            row1.CreateCell(21).SetCellValue("客户归属变更时间");
            row1.CreateCell(22).SetCellValue("描述");
            row1.CreateCell(23).SetCellValue("备注");

            row1.CreateCell(24).SetCellValue("第1次联系时间");
            row1.CreateCell(25).SetCellValue("联系内容");
            row1.CreateCell(26).SetCellValue("第2次联系时间");
            row1.CreateCell(27).SetCellValue("联系内容");
            row1.CreateCell(28).SetCellValue("第3次联系时间");
            row1.CreateCell(29).SetCellValue("联系内容");
            row1.CreateCell(30).SetCellValue("第4次联系时间");
            row1.CreateCell(31).SetCellValue("联系内容");
            row1.CreateCell(32).SetCellValue("第5次联系时间");
            row1.CreateCell(33).SetCellValue("联系内容");
            row1.CreateCell(34).SetCellValue("第6次联系时间");
            row1.CreateCell(35).SetCellValue("联系内容");
            row1.CreateCell(36).SetCellValue("第7次联系时间");
            row1.CreateCell(37).SetCellValue("联系内容");
            row1.CreateCell(38).SetCellValue("第8次联系时间");
            row1.CreateCell(39).SetCellValue("联系内容");
            row1.CreateCell(40).SetCellValue("第9次联系时间");
            row1.CreateCell(41).SetCellValue("联系内容");
            row1.CreateCell(42).SetCellValue("第10次联系时间");
            row1.CreateCell(43).SetCellValue("联系内容");

            row1.CreateCell(44).SetCellValue("第11次联系时间");
            row1.CreateCell(45).SetCellValue("联系内容");
            row1.CreateCell(46).SetCellValue("第12次联系时间");
            row1.CreateCell(47).SetCellValue("联系内容");
            row1.CreateCell(48).SetCellValue("第13次联系时间");
            row1.CreateCell(49).SetCellValue("联系内容");
            row1.CreateCell(50).SetCellValue("第14次联系时间");
            row1.CreateCell(51).SetCellValue("联系内容");
            row1.CreateCell(52).SetCellValue("第15次联系时间");
            row1.CreateCell(53).SetCellValue("联系内容");
            row1.CreateCell(54).SetCellValue("第16次联系时间");
            row1.CreateCell(55).SetCellValue("联系内容");
            row1.CreateCell(56).SetCellValue("第17次联系时间");
            row1.CreateCell(57).SetCellValue("联系内容");
            row1.CreateCell(58).SetCellValue("第18次联系时间");
            row1.CreateCell(59).SetCellValue("联系内容");
            row1.CreateCell(60).SetCellValue("第19次联系时间");
            row1.CreateCell(61).SetCellValue("联系内容");
            row1.CreateCell(62).SetCellValue("第20次联系时间");
            row1.CreateCell(63).SetCellValue("联系内容");

            //将数据逐步写入sheet1各个行
            for (int i = 0; i < list.Count(); i++)
            {
                NPOI.SS.UserModel.IRow rowtemp = sheet1.CreateRow(i + 1);
                rowtemp.CreateCell(0).SetCellValue(list[i].CustomerID);
                rowtemp.CreateCell(1).SetCellValue(list[i].CorporationName);
                rowtemp.CreateCell(2).SetCellValue(list[i].CustomerName);
                rowtemp.CreateCell(3).SetCellValue(list[i].Tel);
                rowtemp.CreateCell(4).SetCellValue(list[i].Fax);
                rowtemp.CreateCell(5).SetCellValue(list[i].Email);
                rowtemp.CreateCell(6).SetCellValue(list[i].MobileTel);
                rowtemp.CreateCell(7).SetCellValue(list[i].SourseFrom);
                rowtemp.CreateCell(8).SetCellValue(list[i].Status);
                rowtemp.CreateCell(9).SetCellValue(list[i].Industry);
                rowtemp.CreateCell(10).SetCellValue(list[i].Province);
                rowtemp.CreateCell(11).SetCellValue(list[i].City);
                rowtemp.CreateCell(12).SetCellValue(list[i].Address);
                rowtemp.CreateCell(13).SetCellValue(list[i].Department.DEPARTMENTNAME);
                rowtemp.CreateCell(14).SetCellValue(list[i].WebSite);
                rowtemp.CreateCell(15).SetCellValue(list[i].CustomerType);
                rowtemp.CreateCell(16).SetCellValue(list[i].NextContactTime.HasValue ? list[i].NextContactTime.Value : DateTime.MinValue);
                rowtemp.CreateCell(17).SetCellValue(list[i].LastModify.HasValue ? list[i].LastModify.Value : DateTime.MinValue);
                rowtemp.CreateCell(18).SetCellValue(list[i].CreateDate.HasValue ? list[i].CreateDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(19).SetCellValue(list[i].Creator.HasValue ? list[i].Creator.Value : 0);
                rowtemp.CreateCell(20).SetCellValue(list[i].Owner);
                rowtemp.CreateCell(21).SetCellValue(list[i].BelongDateTime.HasValue ? list[i].BelongDateTime.Value : DateTime.MinValue);
                rowtemp.CreateCell(22).SetCellValue(list[i].Description);
                rowtemp.CreateCell(23).SetCellValue(list[i].WebSiteRemark);

                rowtemp.CreateCell(24).SetCellValue(list[i].FirstContactDate.HasValue ? list[i].FirstContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(25).SetCellValue(list[i].FirstRemark);

                rowtemp.CreateCell(26).SetCellValue(list[i].SecondContactDate.HasValue ? list[i].SecondContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(27).SetCellValue(list[i].SecondRemark);

                rowtemp.CreateCell(28).SetCellValue(list[i].ThirdContactDate.HasValue ? list[i].ThirdContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(29).SetCellValue(list[i].ThirdRemark);

                rowtemp.CreateCell(30).SetCellValue(list[i].FourthContactDate.HasValue ? list[i].FourthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(31).SetCellValue(list[i].FourthRemark);

                rowtemp.CreateCell(32).SetCellValue(list[i].FifthContactDate.HasValue ? list[i].FifthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(33).SetCellValue(list[i].FifthRemark);

                rowtemp.CreateCell(34).SetCellValue(list[i].SixthContactDate.HasValue ? list[i].SixthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(35).SetCellValue(list[i].SixthRemark);

                rowtemp.CreateCell(36).SetCellValue(list[i].SeventhContactDate.HasValue ? list[i].SeventhContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(37).SetCellValue(list[i].SeventhRemark);

                rowtemp.CreateCell(38).SetCellValue(list[i].EighthContactDate.HasValue ? list[i].EighthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(39).SetCellValue(list[i].EighthRemark);

                rowtemp.CreateCell(40).SetCellValue(list[i].NinthContactDate.HasValue ? list[i].NinthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(41).SetCellValue(list[i].NinthRemark);

                rowtemp.CreateCell(42).SetCellValue(list[i].TenthContactDate.HasValue ? list[i].TenthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(43).SetCellValue(list[i].TenthRemark);

                rowtemp.CreateCell(44).SetCellValue(list[i].ElevenContactDate.HasValue ? list[i].ElevenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(45).SetCellValue(list[i].ElevenRemark);

                rowtemp.CreateCell(46).SetCellValue(list[i].twelveContactDate.HasValue ? list[i].twelveContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(47).SetCellValue(list[i].twelveRemark);

                rowtemp.CreateCell(48).SetCellValue(list[i].thirteenContactDate.HasValue ? list[i].thirteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(49).SetCellValue(list[i].thirteenRemark);

                rowtemp.CreateCell(50).SetCellValue(list[i].fourteenContactDate.HasValue ? list[i].fourteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(51).SetCellValue(list[i].fourteenRemark);

                rowtemp.CreateCell(52).SetCellValue(list[i].fifteenContactDate.HasValue ? list[i].fifteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(53).SetCellValue(list[i].fifteenRemark);

                rowtemp.CreateCell(54).SetCellValue(list[i].sixteenContactDate.HasValue ? list[i].sixteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(55).SetCellValue(list[i].sixteenRemark);

                rowtemp.CreateCell(56).SetCellValue(list[i].seventeenContactDate.HasValue ? list[i].seventeenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(57).SetCellValue(list[i].seventeenRemark);

                rowtemp.CreateCell(58).SetCellValue(list[i].eighteenContactDate.HasValue ? list[i].eighteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(59).SetCellValue(list[i].eighteenRemark);

                rowtemp.CreateCell(60).SetCellValue(list[i].nineteenContactDate.HasValue ? list[i].nineteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(61).SetCellValue(list[i].nineteenRemark);

                rowtemp.CreateCell(62).SetCellValue(list[i].twentyContactDate.HasValue ? list[i].twentyContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(63).SetCellValue(list[i].twentyRemark);
            }
            // 写入到客户端 
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            book.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms, "application/vnd.ms-excel", DateTime.Today.ToLongDateString() + "_公共池_客户信息.xls");
        }

        [HttpGet]
        public FileResult Export_Waste(string CorporationName, string fromType, string status, string TelePhone, string MobilePhone, int? page)
        {

            var customers = from item in db.Customer
                            where item.PoolStatus.Value == publicTag
                            select item;
            if (!string.IsNullOrEmpty(CorporationName))
            {
                customers = customers.Where(x => x.CorporationName.Contains(CorporationName));
            }
            if (!string.IsNullOrEmpty(TelePhone))
            {
                customers = customers.Where(x => x.Tel.Contains(TelePhone));
            }
            if (!string.IsNullOrEmpty(MobilePhone))
            {
                customers = customers.Where(x => x.MobileTel.Contains(MobilePhone));
            }
           
            if (!string.IsNullOrEmpty(fromType))
            {
                customers = customers.Where(x => x.SourseFrom == fromType);
            }
            if (!string.IsNullOrEmpty(status))
            {
                customers = customers.Where(x => x.Status == status);
            }
          

            //过滤角色数据
            customers = Filter(customers);

            var list = customers.ToList();

            //创建Excel文件的对象
            NPOI.HSSF.UserModel.HSSFWorkbook book = new NPOI.HSSF.UserModel.HSSFWorkbook();
            //添加一个sheet
            NPOI.SS.UserModel.ISheet sheet1 = book.CreateSheet("Sheet1");

            /*
             * SELECT top 100
      [CorporationName] as 公司名称
	  ,[CustomerName] as 联系人
      ,[Tel] as 电话
      ,[Fax] as 传真
	  ,[Email] as 邮箱
      ,[City]as 城市
      ,[MobileTel] as 手机
      ,[SourseFrom] as 来源
	   ,[Status] as 状态
      ,[Industry] as 行业
	   ,[Province] as 省份
      ,[RegisteredCapital] as 城市
      ,[Address] as 地址
      ,[WebSite] as 网址
      ,[DepartmentID] as 归属销售部门
      ,[CustomerType] as 客户类型
      ,[NextContactTime] as 下次联系时间
      ,[LastModify] as 最近修改时间
	  ,[CreateDate] as 创建时间
	  ,[Creator] as 创建人
	   ,[Owner] as 客户归属
	  ,[BelongDateTime] 客户归属变更时间
      ,replace(replace([Description],char(10),' '),char(13),' ') as 描述
	   ,[WebSiteRemark] as 备注
  FROM [dbo].[Customer] 
             */

            //给sheet1添加第一行的头部标题
            NPOI.SS.UserModel.IRow row1 = sheet1.CreateRow(0);
            row1.CreateCell(0).SetCellValue("ID");
            row1.CreateCell(1).SetCellValue("公司名称");
            row1.CreateCell(2).SetCellValue("联系人");
            row1.CreateCell(3).SetCellValue("电话");
            row1.CreateCell(4).SetCellValue("传真");
            row1.CreateCell(5).SetCellValue("邮箱");
            row1.CreateCell(6).SetCellValue("手机");
            row1.CreateCell(7).SetCellValue("来源");
            row1.CreateCell(8).SetCellValue("状态");
            row1.CreateCell(9).SetCellValue("行业");
            row1.CreateCell(10).SetCellValue("省份");
            row1.CreateCell(11).SetCellValue("城市");
            row1.CreateCell(12).SetCellValue("地址");
            row1.CreateCell(13).SetCellValue("归属销售部门");
            row1.CreateCell(14).SetCellValue("网址");
            row1.CreateCell(15).SetCellValue("客户类型");
            row1.CreateCell(16).SetCellValue("下次联系时间");
            row1.CreateCell(17).SetCellValue("最近修改时间");
            row1.CreateCell(18).SetCellValue("创建时间");
            row1.CreateCell(19).SetCellValue("创建人");
            row1.CreateCell(20).SetCellValue("客户归属");
            row1.CreateCell(21).SetCellValue("客户归属变更时间");
            row1.CreateCell(22).SetCellValue("描述");
            row1.CreateCell(23).SetCellValue("备注");

            row1.CreateCell(24).SetCellValue("第1次联系时间");
            row1.CreateCell(25).SetCellValue("联系内容");
            row1.CreateCell(26).SetCellValue("第2次联系时间");
            row1.CreateCell(27).SetCellValue("联系内容");
            row1.CreateCell(28).SetCellValue("第3次联系时间");
            row1.CreateCell(29).SetCellValue("联系内容");
            row1.CreateCell(30).SetCellValue("第4次联系时间");
            row1.CreateCell(31).SetCellValue("联系内容");
            row1.CreateCell(32).SetCellValue("第5次联系时间");
            row1.CreateCell(33).SetCellValue("联系内容");
            row1.CreateCell(34).SetCellValue("第6次联系时间");
            row1.CreateCell(35).SetCellValue("联系内容");
            row1.CreateCell(36).SetCellValue("第7次联系时间");
            row1.CreateCell(37).SetCellValue("联系内容");
            row1.CreateCell(38).SetCellValue("第8次联系时间");
            row1.CreateCell(39).SetCellValue("联系内容");
            row1.CreateCell(40).SetCellValue("第9次联系时间");
            row1.CreateCell(41).SetCellValue("联系内容");
            row1.CreateCell(42).SetCellValue("第10次联系时间");
            row1.CreateCell(43).SetCellValue("联系内容");

            row1.CreateCell(44).SetCellValue("第11次联系时间");
            row1.CreateCell(45).SetCellValue("联系内容");
            row1.CreateCell(46).SetCellValue("第12次联系时间");
            row1.CreateCell(47).SetCellValue("联系内容");
            row1.CreateCell(48).SetCellValue("第13次联系时间");
            row1.CreateCell(49).SetCellValue("联系内容");
            row1.CreateCell(50).SetCellValue("第14次联系时间");
            row1.CreateCell(51).SetCellValue("联系内容");
            row1.CreateCell(52).SetCellValue("第15次联系时间");
            row1.CreateCell(53).SetCellValue("联系内容");
            row1.CreateCell(54).SetCellValue("第16次联系时间");
            row1.CreateCell(55).SetCellValue("联系内容");
            row1.CreateCell(56).SetCellValue("第17次联系时间");
            row1.CreateCell(57).SetCellValue("联系内容");
            row1.CreateCell(58).SetCellValue("第18次联系时间");
            row1.CreateCell(59).SetCellValue("联系内容");
            row1.CreateCell(60).SetCellValue("第19次联系时间");
            row1.CreateCell(61).SetCellValue("联系内容");
            row1.CreateCell(62).SetCellValue("第20次联系时间");
            row1.CreateCell(63).SetCellValue("联系内容");

            //将数据逐步写入sheet1各个行
            for (int i = 0; i < list.Count(); i++)
            {
                NPOI.SS.UserModel.IRow rowtemp = sheet1.CreateRow(i + 1);
                rowtemp.CreateCell(0).SetCellValue(list[i].CustomerID);
                rowtemp.CreateCell(1).SetCellValue(list[i].CorporationName);
                rowtemp.CreateCell(2).SetCellValue(list[i].CustomerName);
                rowtemp.CreateCell(3).SetCellValue(list[i].Tel);
                rowtemp.CreateCell(4).SetCellValue(list[i].Fax);
                rowtemp.CreateCell(5).SetCellValue(list[i].Email);
                rowtemp.CreateCell(6).SetCellValue(list[i].MobileTel);
                rowtemp.CreateCell(7).SetCellValue(list[i].SourseFrom);
                rowtemp.CreateCell(8).SetCellValue(list[i].Status);
                rowtemp.CreateCell(9).SetCellValue(list[i].Industry);
                rowtemp.CreateCell(10).SetCellValue(list[i].Province);
                rowtemp.CreateCell(11).SetCellValue(list[i].City);
                rowtemp.CreateCell(12).SetCellValue(list[i].Address);
                rowtemp.CreateCell(13).SetCellValue(list[i].Department.DEPARTMENTNAME);
                rowtemp.CreateCell(14).SetCellValue(list[i].WebSite);
                rowtemp.CreateCell(15).SetCellValue(list[i].CustomerType);
                rowtemp.CreateCell(16).SetCellValue(list[i].NextContactTime.HasValue ? list[i].NextContactTime.Value : DateTime.MinValue);
                rowtemp.CreateCell(17).SetCellValue(list[i].LastModify.HasValue ? list[i].LastModify.Value : DateTime.MinValue);
                rowtemp.CreateCell(18).SetCellValue(list[i].CreateDate.HasValue ? list[i].CreateDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(19).SetCellValue(list[i].Creator.HasValue ? list[i].Creator.Value : 0);
                rowtemp.CreateCell(20).SetCellValue(list[i].Owner);
                rowtemp.CreateCell(21).SetCellValue(list[i].BelongDateTime.HasValue ? list[i].BelongDateTime.Value : DateTime.MinValue);
                rowtemp.CreateCell(22).SetCellValue(list[i].Description);
                rowtemp.CreateCell(23).SetCellValue(list[i].WebSiteRemark);

                rowtemp.CreateCell(24).SetCellValue(list[i].FirstContactDate.HasValue ? list[i].FirstContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(25).SetCellValue(list[i].FirstRemark);

                rowtemp.CreateCell(26).SetCellValue(list[i].SecondContactDate.HasValue ? list[i].SecondContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(27).SetCellValue(list[i].SecondRemark);

                rowtemp.CreateCell(28).SetCellValue(list[i].ThirdContactDate.HasValue ? list[i].ThirdContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(29).SetCellValue(list[i].ThirdRemark);

                rowtemp.CreateCell(30).SetCellValue(list[i].FourthContactDate.HasValue ? list[i].FourthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(31).SetCellValue(list[i].FourthRemark);

                rowtemp.CreateCell(32).SetCellValue(list[i].FifthContactDate.HasValue ? list[i].FifthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(33).SetCellValue(list[i].FifthRemark);

                rowtemp.CreateCell(34).SetCellValue(list[i].SixthContactDate.HasValue ? list[i].SixthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(35).SetCellValue(list[i].SixthRemark);

                rowtemp.CreateCell(36).SetCellValue(list[i].SeventhContactDate.HasValue ? list[i].SeventhContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(37).SetCellValue(list[i].SeventhRemark);

                rowtemp.CreateCell(38).SetCellValue(list[i].EighthContactDate.HasValue ? list[i].EighthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(39).SetCellValue(list[i].EighthRemark);

                rowtemp.CreateCell(40).SetCellValue(list[i].NinthContactDate.HasValue ? list[i].NinthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(41).SetCellValue(list[i].NinthRemark);

                rowtemp.CreateCell(42).SetCellValue(list[i].TenthContactDate.HasValue ? list[i].TenthContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(43).SetCellValue(list[i].TenthRemark);

                rowtemp.CreateCell(44).SetCellValue(list[i].ElevenContactDate.HasValue ? list[i].ElevenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(45).SetCellValue(list[i].ElevenRemark);

                rowtemp.CreateCell(46).SetCellValue(list[i].twelveContactDate.HasValue ? list[i].twelveContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(47).SetCellValue(list[i].twelveRemark);

                rowtemp.CreateCell(48).SetCellValue(list[i].thirteenContactDate.HasValue ? list[i].thirteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(49).SetCellValue(list[i].thirteenRemark);

                rowtemp.CreateCell(50).SetCellValue(list[i].fourteenContactDate.HasValue ? list[i].fourteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(51).SetCellValue(list[i].fourteenRemark);

                rowtemp.CreateCell(52).SetCellValue(list[i].fifteenContactDate.HasValue ? list[i].fifteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(53).SetCellValue(list[i].fifteenRemark);

                rowtemp.CreateCell(54).SetCellValue(list[i].sixteenContactDate.HasValue ? list[i].sixteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(55).SetCellValue(list[i].sixteenRemark);

                rowtemp.CreateCell(56).SetCellValue(list[i].seventeenContactDate.HasValue ? list[i].seventeenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(57).SetCellValue(list[i].seventeenRemark);

                rowtemp.CreateCell(58).SetCellValue(list[i].eighteenContactDate.HasValue ? list[i].eighteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(59).SetCellValue(list[i].eighteenRemark);

                rowtemp.CreateCell(60).SetCellValue(list[i].nineteenContactDate.HasValue ? list[i].nineteenContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(61).SetCellValue(list[i].nineteenRemark);

                rowtemp.CreateCell(62).SetCellValue(list[i].twentyContactDate.HasValue ? list[i].twentyContactDate.Value : DateTime.MinValue);
                rowtemp.CreateCell(63).SetCellValue(list[i].twentyRemark);
            }
            // 写入到客户端 
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            book.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms, "application/vnd.ms-excel", DateTime.Today.ToLongDateString() + "_废弃池_客户信息.xls");
        }

        
    }

    public class Operator
    {
        public string CustomerIds { get; set; }
        public long? Owner { get; set; }
        public string Status { get; set; }
        public string NextContractTime { get; set; }
    }
}
