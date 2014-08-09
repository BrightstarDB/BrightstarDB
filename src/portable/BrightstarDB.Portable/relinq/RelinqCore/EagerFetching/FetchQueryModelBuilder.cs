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
using System.Linq;
using Remotion.Linq.Utilities;

namespace Remotion.Linq.EagerFetching
{
  /// <summary>
  /// Holds a <see cref="FetchRequestBase"/>, a <see cref="SourceItemQueryModel"/> for which the fetch request was created, and the position
  /// where the <see cref="FetchRequestBase"/> occurred in the <see cref="Remotion.Linq.QueryModel.ResultOperators"/> list of the <see cref="SourceItemQueryModel"/>. From
  /// this information, it builds a new <see cref="SourceItemQueryModel"/> that represents the <see cref="FetchRequestBase"/> as a query.
  /// </summary>
  /// <remarks>
  /// Use <see cref="FetchFilteringQueryModelVisitor"/> to retrieve the <see cref="FetchQueryModelBuilder"/> instances for a <see cref="SourceItemQueryModel"/>.
  /// </remarks>
  public class FetchQueryModelBuilder
  {
    private QueryModel _cachedFetchModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FetchQueryModelBuilder"/> class.
    /// </summary>
    /// <param name="fetchRequest">The fetch request.</param>
    /// <param name="queryModel">The query model for which the <paramref name="fetchRequest"/> was originally defined.</param>
    /// <param name="resultOperatorPosition">The result operator position where the <paramref name="fetchRequest"/> was originally located.
    /// The <see cref="FetchQueryModelBuilder"/> will include all result operators prior to this position into the fetch <see cref="SourceItemQueryModel"/>,
    /// but it will not include any result operators occurring after (or at) that position.</param>
    public FetchQueryModelBuilder (FetchRequestBase fetchRequest, QueryModel queryModel, int resultOperatorPosition)
    {
      ArgumentUtility.CheckNotNull ("fetchRequest", fetchRequest);
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);

      FetchRequest = fetchRequest;
      SourceItemQueryModel = queryModel;
      ResultOperatorPosition = resultOperatorPosition;
    }

    public FetchRequestBase FetchRequest { get; private set; }
    public QueryModel SourceItemQueryModel { get; private set; }
    public int ResultOperatorPosition { get; private set; }

    /// <summary>
    /// Creates the fetch query model for the <see cref="FetchRequestBase"/>, caching the result.
    /// </summary>
    /// <returns>
    /// A new <see cref="SourceItemQueryModel"/> which represents the same query as <see cref="SourceItemQueryModel"/> but selecting
    /// the objects described by <see cref="FetchRequestBase.RelationMember"/> instead of the objects selected by the 
    /// <see cref="SourceItemQueryModel"/>. From the original <see cref="SourceItemQueryModel"/>, only those result operators are included that occur
    /// prior to <see cref="ResultOperatorPosition"/>.
    /// </returns>
    public QueryModel GetOrCreateFetchQueryModel ()
    {
      if (_cachedFetchModel == null)
      {
        var sourceItemModel = SourceItemQueryModel.Clone();
        sourceItemModel.ResultTypeOverride = null;

        int resultOperatorsToDelete = sourceItemModel.ResultOperators.Count - ResultOperatorPosition;
        for (int i = 0; i < resultOperatorsToDelete; ++i)
          sourceItemModel.ResultOperators.RemoveAt (ResultOperatorPosition);

        _cachedFetchModel = FetchRequest.CreateFetchQueryModel (sourceItemModel);
      }

      return _cachedFetchModel;
    }

    /// <summary>
    /// Creates <see cref="FetchQueryModelBuilder"/> objects for the <see cref="FetchRequestBase.InnerFetchRequests"/> of the 
    /// <see cref="FetchRequest"/>. Inner fetch requests start from the fetch query model of the outer fetch request, and they have
    /// a <see cref="ResultOperatorPosition"/> of 0.
    /// </summary>
    /// <returns>An array of <see cref="FetchQueryModelBuilder"/> objects for the <see cref="FetchRequestBase.InnerFetchRequests"/> of the
    /// <see cref="FetchRequest"/>.</returns>
    public FetchQueryModelBuilder[] CreateInnerBuilders ()
    {
      var innerBuilders = FetchRequest.InnerFetchRequests.Select (
          request => new FetchQueryModelBuilder (request, GetOrCreateFetchQueryModel(), 0));
      return innerBuilders.ToArray ();
    }
  }
}
