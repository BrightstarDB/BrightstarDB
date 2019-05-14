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
using System;
using System.Linq.Expressions;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using Remotion.Linq.Utilities;

namespace Remotion.Linq.EagerFetching.Parsing
{
  public class ThenFetchOneExpressionNode : FetchExpressionNodeBase
  {
    public ThenFetchOneExpressionNode (MethodCallExpressionParseInfo parseInfo, LambdaExpression relatedObjectSelector)
        : base (parseInfo, ArgumentUtility.CheckNotNull ("relatedObjectSelector", relatedObjectSelector))
    {
    }

    //TODO: Seems not to be used. Possibly redesign
    protected override ResultOperatorBase CreateResultOperator (ClauseGenerationContext clauseGenerationContext)
    {
      throw new NotImplementedException ("Call ApplyNodeSpecificSemantics instead.");
    }

    protected override QueryModel ApplyNodeSpecificSemantics (QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
    {
      var previousFetchRequest = clauseGenerationContext.GetContextInfo (Source) as FetchRequestBase;
      if (previousFetchRequest == null)
        throw new ParserException ("ThenFetchOne must directly follow another Fetch request.");

      FetchRequestBase innerFetchRequest = new FetchOneRequest (RelationMember);
      innerFetchRequest = previousFetchRequest.GetOrAddInnerFetchRequest (innerFetchRequest);
      clauseGenerationContext.AddContextInfo (this, innerFetchRequest);

      return queryModel;
    }
  }
}
