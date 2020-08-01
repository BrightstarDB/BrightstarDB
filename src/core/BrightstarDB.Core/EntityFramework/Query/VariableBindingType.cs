namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// An enumeration used to specify how a SPARQL variable is bound in a <see cref="SelectVariableNameExpression"/>
    /// </summary>
    internal enum VariableBindingType
    {
        Resource,
        Literal
    }
}