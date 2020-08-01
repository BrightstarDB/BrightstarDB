namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// Extends <see cref="SelectVariableNameExpression"/> to also store the configured identifier prefix
    /// to use for trimming the resource URI down to an Id when returning results into a LINQ context.
    /// </summary>
    internal class SelectIdentifierVariableNameExpression : SelectVariableNameExpression
    {
        public string IdentifierPrefix { get; private set; }

        public SelectIdentifierVariableNameExpression(string sourceVarName, string prefix) : base(sourceVarName, VariableBindingType.Resource, typeof(string))
        {
            IdentifierPrefix = prefix;
        }
    }
}
