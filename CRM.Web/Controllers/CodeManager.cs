using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.ObjectModel;

namespace CRM.Web.Controllers
{
    public class CodeDescription
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Category{get;set;}

        public CodeDescription(string code, string description, string category)
        {
            this.Code = code;
            this.Description = description;
            this.Category = category;
        }
    }
    public static class CodeManager
    {
        private static CodeDescription[] codes = new CodeDescription[]
        {
            //new CodeDescription("M","Male","Gender"),
            //new CodeDescription("F","Female","Gender"),
            //new CodeDescription("S","Single","MaritalStatus"),
            //new CodeDescription("M","Married","MaritalStatus"),
            //new CodeDescription("CN","China","Country"),
            //new CodeDescription("US","Unite States","Country"),
            //new CodeDescription("UK","Britain","Country"),
            //new CodeDescription("SG","Singapore","Country"),

            new CodeDescription("新签","新签","CustomerStatus"),
            new CodeDescription("续签","续签","CustomerStatus"),

            new CodeDescription("A","A","Idea"),
            new CodeDescription("B","B","Idea"),
            new CodeDescription("C","C","Idea"),
            new CodeDescription("D","D","Idea"),
            new CodeDescription("自选","自选","Idea"),

            new CodeDescription("未审核","未审核","CheckStatus"),
            new CodeDescription("已审核","已审核","CheckStatus"),


            new CodeDescription("掘金","掘金","Product"),
            new CodeDescription("锦囊","锦囊","Product"),
            new CodeDescription("仅百度","仅百度","Product"),

            new CodeDescription("百度开户","百度开户","Product"),
            new CodeDescription("360开户","360开户","Product"),
            new CodeDescription("搜搜开户","搜搜开户","Product"),
            new CodeDescription("搜狗开户","搜狗开户","Product"),
            new CodeDescription("有道开户","有道开户","Product"),

            new CodeDescription("未跟进","未跟进","ShopStatus"),
            new CodeDescription("收集材料","收集材料","ShopStatus"),
            new CodeDescription("制作中","制作中","ShopStatus"),
            new CodeDescription("制作完成","制作完成","ShopStatus"),

            new CodeDescription("开通","开通","OrderStatus"),
            new CodeDescription("已上线","已上线","OrderStatus"),
            new CodeDescription("暂停","暂停","OrderStatus"),
            new CodeDescription("结束","结束","OrderStatus"),
            new CodeDescription("退款","退款","OrderStatus")
        };
        public static Collection<CodeDescription> GetCodes(string category)
        {
            Collection<CodeDescription> codeCollection = new Collection<CodeDescription>();
            foreach(var code in codes.Where(code=>code.Category == category))
            {
                codeCollection.Add(code);
            }
            return codeCollection;
        }
    }
}