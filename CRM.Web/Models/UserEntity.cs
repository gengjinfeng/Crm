using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Models
{
    public class UserEntity
    {
        public long USERID { get; set; }
        public long ROLEID { get; set; }
        public string USERNAME { get; set; }
        public string PHONE { get; set; }
        public string EMAIL { get; set; }
        public string PASSWORD { get; set; }
        public long DEPARTMENTID { get; set; }
        public Nullable<byte> USERTYPE { get; set; }
        public String USERSTATE { get; set; }
        public string MOBILEPHONE { get; set; }
        public Nullable<System.DateTime> TR { get; set; }

        public string ROLE { get; set; }

        public string DEPARTMENT { get; set; }

        public string StatusCN { 
            get {
                if (USERSTATE == null)
                {
                    return "无状态";
                }
                return USERSTATE=="1" ? "启用" : "禁用"; } 
        }


        public string OldPassword { get; set; }

        public string NewPassword { get; set; }
        
    }
}
