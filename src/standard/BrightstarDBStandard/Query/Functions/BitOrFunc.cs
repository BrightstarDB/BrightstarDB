using System;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using VDS.RDF.Query.Expressions;

namespace BrightstarDB.Query.Functions
{
    internal class BitOrFunc : BaseBinaryExpression
    {
        public BitOrFunc(ISparqlExpression arg1, ISparqlExpression arg2)
            : base(arg1, arg2)
        {
        }

        #region Overrides of BaseBinaryExpression

        public override IValuedNode Evaluate(SparqlEvaluationContext context, int bindingId)
        {
            IValuedNode a = _leftExpr.Evaluate(context, bindingId);
            IValuedNode b = _rightExpr.Evaluate(context, bindingId);
            var type = (SparqlNumericType)Math.Max((int)a.NumericType, (int)b.NumericType);
            if (type == SparqlNumericType.Integer)
            {
                return new LongNode(null, a.AsInteger() | b.AsInteger());
            }
            throw new RdfQueryException("Cannot evaluate bitwise OR expression as the arguments are not integer values.");
        }

        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new BitAndFunc(transformer.Transform(_leftExpr), transformer.Transform(_rightExpr));
        }

        public override SparqlExpressionType Type
        {
            get { return SparqlExpressionType.Function; }
        }

        public override string Functor
        {
            get { return BrightstarFunctionFactory.BrightstarFunctionsNamespace + BrightstarFunctionFactory.BitOr; }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return "<" + BrightstarFunctionFactory.BrightstarFunctionsNamespace + BrightstarFunctionFactory.BitOr +
                   ">(" + _leftExpr + ", " + _rightExpr + ")";
        }

        #endregion
    }
}