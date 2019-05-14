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
using System.Reflection;
using Remotion.Linq.Utilities;

namespace Remotion.Linq.Parsing.Structure.NodeTypeProviders
{
  /// <summary>
  /// Defines a name and a filter predicate used when determining the matching expression node type by <see cref="MethodNameBasedNodeTypeRegistry"/>.
  /// </summary>
  public class NameBasedRegistrationInfo
  {
    private readonly string _name;
    private readonly Func<MethodInfo, bool> _filter;

    public NameBasedRegistrationInfo (string name, Func<MethodInfo, bool> filter) 
    {
      ArgumentUtility.CheckNotNullOrEmpty ("name", name);
      ArgumentUtility.CheckNotNull ("filter", filter);

      _name = name;
      _filter = filter;
    }

    public string Name
    {
      get { return _name; }
    }

    public Func<MethodInfo, bool> Filter
    {
      get { return _filter; }
    }
  }
}