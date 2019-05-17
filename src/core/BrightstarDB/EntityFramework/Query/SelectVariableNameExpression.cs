using System;
using System.Linq.Expressions;


namespace BrightstarDB.EntityFramework.Query
{
    internal class SelectVariableNameExpression : Expression
    {
        public SelectVariableNameExpression(string varName, VariableBindingType bindingType, Type resultType)
        {
            Name = varName;
            BindingType = bindingType;
            Type = resultType;
        }

        public string Name { get; set; }
        public VariableBindingType BindingType { get; set; }

        public override Type Type { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;


        #region Overrides of Expression

        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            return this;
        }

        #endregion

    }
}