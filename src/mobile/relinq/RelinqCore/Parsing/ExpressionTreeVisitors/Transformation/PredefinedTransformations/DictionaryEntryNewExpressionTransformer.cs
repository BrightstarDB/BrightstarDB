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
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Remotion.Linq.Parsing.ExpressionTreeVisitors.Transformation.PredefinedTransformations
{
  /// <summary>
  /// Detects <see cref="NewExpression"/> nodes for <see cref="DictionaryEntry"/> and adds <see cref="MemberInfo"/> metadata to those nodes.
  /// This allows LINQ providers to match member access and constructor arguments more easily.
  /// </summary>
  public class DictionaryEntryNewExpressionTransformer : MemberAddingNewExpressionTransformerBase
  {
    protected override MemberInfo[] GetMembers (ConstructorInfo constructorInfo, ReadOnlyCollection<Expression> arguments)
    {
      return new[] 
      { 
          GetMemberForNewExpression (constructorInfo.DeclaringType, "Key"), 
          GetMemberForNewExpression (constructorInfo.DeclaringType, "Value") 
      };
    }

    protected override bool CanAddMembers (Type instantiatedType, ReadOnlyCollection<Expression> arguments)
    {
      return instantiatedType == typeof (DictionaryEntry) && arguments.Count == 2;
    }
  }
}