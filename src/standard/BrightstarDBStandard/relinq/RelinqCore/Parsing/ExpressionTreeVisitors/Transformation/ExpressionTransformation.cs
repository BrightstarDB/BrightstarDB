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

namespace Remotion.Linq.Parsing.ExpressionTreeVisitors.Transformation
{
  /// <summary>
  /// Transforms a given <see cref="Expression"/>. If the <see cref="ExpressionTransformation"/> can handle the <see cref="Expression"/>,
  /// it should return a new, transformed <see cref="Expression"/> instance. Otherwise, it should return the input <paramref name="expression"/> 
  /// instance.
  /// </summary>
  /// <param name="expression">The expression to be transformed.</param>
  /// <returns>The result of the transformation, or <paramref name="expression"/> if no transformation was applied.</returns>
  public delegate Expression ExpressionTransformation (Expression expression);
}