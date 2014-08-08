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
using System.Linq.Expressions;

namespace Remotion.Linq.EagerFetching
{
  /// <summary>
  /// Provides a fluent interface to recursively fetch related objects of objects which themselves are eager-fetched. All query methods
  /// are implemented as extension methods.
  /// </summary>
  /// <typeparam name="TQueried">The type of the objects returned by the query.</typeparam>
  /// <typeparam name="TFetch">The type of object from which the recursive fetch operation should be made.</typeparam>
// ReSharper disable UnusedTypeParameter
  public class FluentFetchRequest<TQueried, TFetch> : QueryableBase<TQueried>
  {
    public FluentFetchRequest (IQueryProvider provider, Expression expression)
        : base (provider, expression)
    {
    }
  }
  // ReSharper restore UnusedTypeParameter
}
