// This file is part of the re-linq project (relinq.codeplex.com)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// re-linq is free software; you can redistribute it and/or modify it under 
// the terms of the GNU Lesser General Public License as published by the 
// Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
// 
// re-linq is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-linq; if not, see http://www.gnu.org/licenses.
// 
using System.Linq.Expressions;
using Remotion.Linq.Parsing.ExpressionTreeVisitors;
using Remotion.Linq.Utilities;

namespace Remotion.Linq.Parsing.Structure.IntermediateModel
{
  /// <summary>
  /// Resolves an expression using <see cref="IExpressionNode.Resolve"/>, removing transparent identifiers and detecting subqueries
  /// in the process. This is used by methods such as <see cref="SelectExpressionNode.GetResolvedSelector"/>, which are
  /// used when a clause is created from an <see cref="IExpressionNode"/>.
  /// </summary>
  public class ExpressionResolver
  {
    public ExpressionResolver (IExpressionNode currentNode)
    {
      ArgumentUtility.CheckNotNull ("currentNode", currentNode);

      CurrentNode = currentNode;
    }

    public IExpressionNode CurrentNode { get; set; }

    public Expression GetResolvedExpression (
        Expression unresolvedExpression, ParameterExpression parameterToBeResolved, ClauseGenerationContext clauseGenerationContext)
    {
      ArgumentUtility.CheckNotNull ("unresolvedExpression", unresolvedExpression);
      ArgumentUtility.CheckNotNull ("parameterToBeResolved", parameterToBeResolved);

      var sourceNode = CurrentNode.Source;
      var resolvedExpression = sourceNode.Resolve (parameterToBeResolved, unresolvedExpression, clauseGenerationContext);
      resolvedExpression = TransparentIdentifierRemovingExpressionTreeVisitor.ReplaceTransparentIdentifiers (resolvedExpression);
      return resolvedExpression;
    }
  }
}
