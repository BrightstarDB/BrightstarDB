using System;
using System.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace BrightstarDB.EntityFramework.Query
{
    internal class SelectVariableNameExpression : ExtensionExpression 
    {
        public SelectVariableNameExpression(string varName, VariableBindingType bindingType, Type resultType) : 
#if WINDOWS_PHONE || PORTABLE
            base(resultType) // Results in an Extension ExpressionType (according to the docs)
#else
            base(resultType, ExpressionType.Extension)
#endif
        {
            Name = varName;
            BindingType = bindingType;
        }

        public string Name { get; set; }
        public VariableBindingType BindingType { get; set; }

#if !WINDOWS_PHONE && !PORTABLE
        public override ExpressionType NodeType
        {
            get
            {
                return ExpressionType.Extension;
            }
        }
#endif

        #region Overrides of ExtensionExpression

        /// <summary>
        /// Must be overridden by <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/> subclasses by calling <see cref="M:Remotion.Linq.Parsing.ExpressionTreeVisitor.VisitExpression(System.Linq.Expressions.Expression)"/> on all 
        ///             children of this extension node. 
        /// </summary>
        /// <param name="visitor">The visitor to visit the child nodes with.</param>
        /// <returns>
        /// This <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/>, or an expression that should replace it in the surrounding tree.
        /// </returns>
        /// <remarks>
        /// If the visitor replaces any of the child nodes, a new <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/> instance should
        ///             be returned holding the new child nodes. If the node has no children or the visitor does not replace any child node, the method should
        ///             return this <see cref="T:Remotion.Linq.Clauses.Expressions.ExtensionExpression"/>. 
        /// </remarks>
        protected internal override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            return this;
        }

        #endregion
    }
}
