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
    
    public partial class Order
    {
        public int Id { get; set; }
        public string Period { get; set; }
        public string Keyword { get; set; }
        public string KeywordAttach { get; set; }
        public string Url { get; set; }
        public string IDCode { get; set; }
        public string TemplateCode { get; set; }
        public string Idea { get; set; }
        public string BaiduAccount { get; set; }
        public string BaiduPwd { get; set; }
        public string BaiduAnswer { get; set; }
        public string OrderRemark { get; set; }
        public string ContractCode { get; set; }
        public string ContractMoney { get; set; }
        public string Product { get; set; }
        public string ShopStatus { get; set; }
        public string ShopAddress { get; set; }
        public string FollowUpPerson { get; set; }
        public Nullable<System.DateTime> ShopMakeStarttime { get; set; }
        public Nullable<System.DateTime> ShopMakeEndtime { get; set; }
        public string ShopRemark { get; set; }
        public string OrderStatus { get; set; }
        public Nullable<System.DateTime> GuideStarttime { get; set; }
        public Nullable<System.DateTime> GuideEndtime { get; set; }
        public Nullable<System.DateTime> NuggetStarttime { get; set; }
        public Nullable<System.DateTime> NuggetEndtime { get; set; }
        public string OperationRemark { get; set; }
        public string CustomerStatus { get; set; }
        public long CustomerId { get; set; }
        public long UserId { get; set; }
        public string SignBill { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> LastModifyTime { get; set; }
        public string SalesPerformance { get; set; }
        public Nullable<System.DateTime> BaiduStarttime { get; set; }
        public Nullable<System.DateTime> BaiduEndtime { get; set; }
        public Nullable<System.DateTime> BaiduOpenStarttime { get; set; }
        public Nullable<System.DateTime> BaiduOpenEndtime { get; set; }
        public Nullable<System.DateTime> Qihu360OpenStarttime { get; set; }
        public Nullable<System.DateTime> Qihu360OpenEndtime { get; set; }
        public Nullable<System.DateTime> SosoOpenStarttime { get; set; }
        public Nullable<System.DateTime> SosoOpenEndtime { get; set; }
        public Nullable<System.DateTime> SougouOpenStarttime { get; set; }
        public Nullable<System.DateTime> SougouOpenEndtime { get; set; }
        public Nullable<System.DateTime> YoudaoOpenStarttime { get; set; }
        public Nullable<System.DateTime> YoudaoOpenEndtime { get; set; }
        public string OrderLinktor { get; set; }
        public string OrderTelePhone { get; set; }
        public string OrderMobilePhone { get; set; }
        public string OrderFax { get; set; }
    
        public virtual Customer Customer { get; set; }
    }
}
