using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface IIdentityClashTest
    {
        string Id { get; }
    }

    [Entity]
    public interface IIdentityClashTestLevel1 : IIdentityClashTest
    {
        //[Identifier]
        //string MyId { get; }
    }

    [Entity]
    public interface IIdentityClashTestLevel2 : IIdentityClashTestLevel1
    {
        //[Identifier]
        //string MyId { get; }
    }

}
