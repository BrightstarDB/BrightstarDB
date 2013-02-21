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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Utilities;

namespace Remotion.Linq.EagerFetching
{
  /// <summary>
  /// Base class for classes representing a property that should be eager-fetched when a query is executed.
  /// </summary>
  public abstract class FetchRequestBase : SequenceTypePreservingResultOperatorBase
  {
    private readonly FetchRequestCollection _innerFetchRequestCollection = new FetchRequestCollection();

    private MemberInfo _relationMember;

    protected FetchRequestBase (MemberInfo relationMember)
    {
      ArgumentUtility.CheckNotNull ("relationMember", relationMember);
      _relationMember = relationMember;
    }

    /// <summary>
    /// Gets the <see cref="MemberInfo"/> of the relation member whose contained object(s) should be fetched.
    /// </summary>
    /// <value>The relation member.</value>
    public MemberInfo RelationMember
    {
      get { return _relationMember; }
      set { _relationMember = ArgumentUtility.CheckNotNull ("value", value); }
    }

    /// <summary>
    /// Gets the inner fetch requests that were issued for this <see cref="FetchRequestBase"/>.
    /// </summary>
    /// <value>The fetch requests added via <see cref="GetOrAddInnerFetchRequest"/>.</value>
    public IEnumerable<FetchRequestBase> InnerFetchRequests
    {
      get { return _innerFetchRequestCollection.FetchRequests; }
    }

    /// <summary>
    /// Gets a the fetch query model, i.e. a new <see cref="QueryModel"/> that incorporates a given <paramref name="sourceItemQueryModel"/> as a
    /// <see cref="SubQueryExpression"/> and selects the fetched items from it.
    /// </summary>
    /// <param name="sourceItemQueryModel">A <see cref="QueryModel"/> that yields the source items for which items are to be fetched.</param>
    /// <returns>A <see cref="QueryModel"/> that selects the fetched items from <paramref name="sourceItemQueryModel"/> as a subquery.</returns>
    /// <remarks>
    /// This method does not clone the <paramref name="sourceItemQueryModel"/>, remove result operatores, etc. Use 
    /// <see cref="FetchQueryModelBuilder.GetOrCreateFetchQueryModel"/> (via <see cref="FetchFilteringQueryModelVisitor"/>) for the full algorithm.
    /// </remarks>
    public virtual QueryModel CreateFetchQueryModel (QueryModel sourceItemQueryModel)
    {
      ArgumentUtility.CheckNotNull ("sourceItemQueryModel", sourceItemQueryModel);

      var sourceItemName = sourceItemQueryModel.GetNewName ("#fetch");
      QueryModel fetchQueryModel;
      try
      {
        fetchQueryModel = sourceItemQueryModel.ConvertToSubQuery (sourceItemName);
      }
      catch (InvalidOperationException ex)
      {
        var message = string.Format (
            "The given source query model cannot be used to fetch the relation member '{0}': {1}",
            RelationMember.Name,
            ex.Message);
        throw new ArgumentException (message, ex);
      }

      if (!RelationMember.DeclaringType.IsAssignableFrom (fetchQueryModel.MainFromClause.ItemType))
      {
        var message = string.Format (
            "The given source query model selects items that do not match the fetch request. In order to fetch the relation member '{0}', the query "
            + "must yield objects of type '{1}', but it yields '{2}'.",
            RelationMember.Name,
            RelationMember.DeclaringType,
            fetchQueryModel.MainFromClause.ItemType);
        throw new ArgumentException (message, "sourceItemQueryModel");
      }

      ModifyFetchQueryModel (fetchQueryModel);

      return fetchQueryModel;
    }

    /// <summary>
    /// Modifies the given query model for fetching, adding new <see cref="AdditionalFromClause"/> instances and changing the 
    /// <see cref="SelectClause"/> as needed.
    /// This method is called by <see cref="CreateFetchQueryModel"/> in the process of creating the new fetch query model.
    /// </summary>
    /// <param name="fetchQueryModel">The fetch query model to modify.</param>
    protected abstract void ModifyFetchQueryModel (QueryModel fetchQueryModel);
    
    /// <summary>
    /// Gets or adds an inner eager-fetch request for this <see cref="FetchRequestBase"/>.
    /// </summary>
    /// <param name="fetchRequest">The <see cref="FetchRequestBase"/> to be added.</param>
    /// <returns>
    /// <paramref name="fetchRequest"/> or, if another <see cref="FetchRequestBase"/> for the same relation member already existed,
    /// the existing <see cref="FetchRequestBase"/>.
    /// </returns>
    public FetchRequestBase GetOrAddInnerFetchRequest (FetchRequestBase fetchRequest)
    {
      ArgumentUtility.CheckNotNull ("fetchRequest", fetchRequest);
      return _innerFetchRequestCollection.GetOrAddFetchRequest (fetchRequest);
    }

    public override StreamedSequence ExecuteInMemory<T> (StreamedSequence input)
    {
      ArgumentUtility.CheckNotNull ("input", input);
      return input;
    }

    public override string ToString ()
    {
      var result = string.Format ("Fetch ({0}.{1})", _relationMember.DeclaringType.Name, _relationMember.Name);
      var innerResults = InnerFetchRequests.Select (request => request.ToString ());
      return SeparatedStringBuilder.Build (".Then", new[] { result }.Concat (innerResults));
    }
  }
}
