using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

namespace BrightstarDB.Azure.Gateway
{
    public static class ExtensionMethods
    {
        public static MvcHtmlString DescriptionFor<TModel, TProperty>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression)
        {
            ModelMetadata modelMetadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            if(!String.IsNullOrEmpty(modelMetadata.Description))
            {
                return new MvcHtmlString(modelMetadata.Description);
            }
            return new MvcHtmlString(String.Empty);
        }
    }
}