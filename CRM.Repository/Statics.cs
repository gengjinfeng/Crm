using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Repository
{
    public class CRMStatics
    {
        public System.Data.Entity.Infrastructure.DbRawSqlQuery<Statics> RecordItem { get; set; }

        public System.Data.Entity.Infrastructure.DbRawSqlQuery<Statics> RecordTotal { get; set; }
    }
    public class Statics
    {
        public string USERNAME { get; set; }

        public string Status { get; set; }

        public Int32 Total { get; set; }
    }

    public class StaticsSearchParams
    {
        public string DepartmentId { get; set; }
        public string Owner { get; set; }

        public string Status { get; set; }

        public string NextContractStartTime { get; set; }

        public string NextContractEndTime { get; set; }
        public string CreateDateStartTime { get; set; }

        public string CreateDateEndTime { get; set; }
    }
}
