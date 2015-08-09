using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Web.UI.WebControls;
using CRM.Web.Controllers;
namespace System.Web.Mvc.Html
{
    public static class ListControlExtensions
    {
        public static MvcHtmlString RadioButtonList(this HtmlHelper htmlHelper, string name, string codeCategory, RepeatDirection repeatDirection = RepeatDirection.Horizontal)
        {
            var codes = CodeManager.GetCodes(codeCategory);
            return ListControlUtil.GenerateHtml(name, codes, repeatDirection, "radio", null);
        }
        public static MvcHtmlString RadioButtonListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string codeCategory, RepeatDirection repeatDirection = RepeatDirection.Horizontal)
        {
            var codes = CodeManager.GetCodes(codeCategory);
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression<TModel, TProperty>(expression, htmlHelper.ViewData);
            string name = ExpressionHelper.GetExpressionText(expression);
            string fullHtmlFieldName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            return ListControlUtil.GenerateHtml(fullHtmlFieldName, codes, repeatDirection, "radio", metadata.Model);
        }

        public static MvcHtmlString CheckBoxList(this HtmlHelper htmlHelper, string name, string codeCategory, RepeatDirection repeatDirection = RepeatDirection.Horizontal)
        {
            var codes = CodeManager.GetCodes(codeCategory);
            return ListControlUtil.GenerateHtml(name, codes, repeatDirection, "checkbox", null);
        }
        public static MvcHtmlString CheckBoxListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string codeCategory, RepeatDirection repeatDirection = RepeatDirection.Horizontal)
        {
            var codes = CodeManager.GetCodes(codeCategory);
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression<TModel, TProperty>(expression, htmlHelper.ViewData);
            string name = ExpressionHelper.GetExpressionText(expression);
            string fullHtmlFieldName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            return ListControlUtil.GenerateHtml(fullHtmlFieldName, codes, repeatDirection, "checkbox", metadata.Model);
        }
    }
    public static class ListControlUtil
    {
        public static MvcHtmlString GenerateHtml(string name, Collection<CodeDescription> codes, RepeatDirection repeatDirection, string type, object stateValue)
        {
            TagBuilder table = new TagBuilder("table");
            int i = 0;
            bool isCheckBox = type == "checkbox";
            if (repeatDirection == RepeatDirection.Horizontal)
            {
                TagBuilder tr = new TagBuilder("tr");
                foreach (var code in codes)
                {
                    i++;
                    string id = string.Format("{0}_{1}", name, i);
                    TagBuilder td = new TagBuilder("td");

                    bool isChecked = false;
                    if (isCheckBox)
                    {
                        IEnumerable<string> currentValues = stateValue as IEnumerable<string>;
                        isChecked = (null != currentValues && currentValues.Contains(code.Code));
                    }
                    else
                    {
                        string currentValue = stateValue as string;
                        isChecked = (null != currentValue && code.Code == currentValue.Trim());
                    }

                    td.InnerHtml = GenerateRadioHtml(name, id, code.Description, code.Code, isChecked, type);
                    tr.InnerHtml += td.ToString();
                }
                table.InnerHtml = tr.ToString();
            }
            else
            {
                foreach (var code in codes)
                {
                    TagBuilder tr = new TagBuilder("tr");
                    i++;
                    string id = string.Format("{0}_{1}", name, i);
                    TagBuilder td = new TagBuilder("td");

                    bool isChecked = false;
                    if (isCheckBox)
                    {
                        IEnumerable<string> currentValues = stateValue as IEnumerable<string>;
                        isChecked = (null != currentValues && currentValues.Contains(code.Code));
                    }
                    else
                    {
                        string currentValue = stateValue as string;
                        isChecked = (null != currentValue && code.Code == currentValue.Trim());
                    }

                    td.InnerHtml = GenerateRadioHtml(name, id, code.Description, code.Code, isChecked, type);
                    tr.InnerHtml = td.ToString();
                    table.InnerHtml += tr.ToString();
                }
            }
            return new MvcHtmlString(table.ToString());
        }

        private static string GenerateRadioHtml(string name, string id, string labelText, string value, bool isChecked, string type)
        {
            StringBuilder sb = new StringBuilder();

            TagBuilder label = new TagBuilder("label");
            label.MergeAttribute("for", id);
            label.SetInnerText(labelText);

            TagBuilder input = new TagBuilder("input");
            input.GenerateId(id);
            input.MergeAttribute("name", name);
            input.MergeAttribute("type", type);
            input.MergeAttribute("value", value);
            if (isChecked)
            {
                input.MergeAttribute("checked", "checked");
            }
            sb.AppendLine(input.ToString());
            sb.AppendLine(label.ToString());
            return sb.ToString();
        }
    }
}