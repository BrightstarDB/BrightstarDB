using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Query.Functions;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Expressions;

namespace BrightstarDB.Query
{
    internal class BrightstarFunctionFactory : ISparqlCustomExpressionFactory
    {
        public const string BrightstarFunctionsNamespace = "http://brightstardb.com/.well-known/sparql/functions/";

        public const string BitAnd = "bit_and";
        public const string BitOr = "bit_or";

        private readonly string[] _functionUris = {
                                            BitAnd, BitOr
                                        };

        #region Implementation of ISparqlCustomExpressionFactory

        public bool TryCreateExpression(Uri u, List<ISparqlExpression> args, Dictionary<string, ISparqlExpression> scalarArguments, out ISparqlExpression expr)
        {
            // If any scalar arguments are present, it can't be a BrightstarDB function
            if (scalarArguments.Count > 0)
            {
                expr = null;
                return false;
            }

            var func = u.ToString();
            if (func.StartsWith(BrightstarFunctionsNamespace))
            {
                func = func.Substring(BrightstarFunctionsNamespace.Length);
                ISparqlExpression brightstarFunc = null;
                switch (func)
                {
                    case BitAnd:
                        if (args.Count == 2)
                        {
                            brightstarFunc = new BitAndFunc(args[0], args[1]);
                        } 
                        else
                        {
                            throw new RdfParseException("Incorrect number of arguments for the BrightstarDB bit_and() function.");
                        }
                        break;
                    case BitOr:
                        if (args.Count == 2)
                        {
                            brightstarFunc = new BitOrFunc(args[0], args[1]);
                        }
                        else
                        {
                            throw new RdfParseException("Incorrect number of arguments for the BrightstarDB bit_and() function.");
                        }
                        break;
                }
                if (brightstarFunc != null)
                {
                    expr = brightstarFunc;
                    return true;
                }
            }
            expr = null;
            return false;
        }

        public IEnumerable<Uri> AvailableExtensionFunctions
        {
            get { return from u in _functionUris select UriFactory.Create(BrightstarFunctionsNamespace + u); }
        }

        public IEnumerable<Uri> AvailableExtensionAggregates
        {
            get { return Enumerable.Empty<Uri>(); }
        }

        #endregion
    }
}