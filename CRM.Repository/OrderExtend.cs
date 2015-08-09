using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Repository
{
    public partial class Order
    {
        public string UserName {
            get
            {
                string userName = string.Empty;
                using (Iso58Entities db = new Iso58Entities())
                {
                    User user = db.User.Where(u => u.USERID == UserId).SingleOrDefault();
                    userName = user.USERNAME;
                }
                return userName;
            }
        }
    }
}
