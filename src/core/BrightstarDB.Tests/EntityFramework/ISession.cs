using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Tests.EntityFramework
{
    [Entity]
    public interface ISession
    {
        string Id { get; }
        string Speaker { get; set; }
    }

    [Entity]
    public interface IEveningSession : ISession
    {
        string DateTime { get; set; }
        bool FreeBeer { get; set; }
    }

    [Entity]
    public interface ITechnicalEveningSession : IEveningSession
    {
        string Subject { get; set; }
    }
}
