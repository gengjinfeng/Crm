//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace CRM.Repository
{
    using System;
    using System.Collections.Generic;
    
    public partial class Permission
    {
        public long ID { get; set; }
        public long ROLEID { get; set; }
        public long PURVIEWID { get; set; }
        public Nullable<System.DateTime> TR { get; set; }
    
        public virtual Function Function { get; set; }
        public virtual Role Role { get; set; }
    }
}
