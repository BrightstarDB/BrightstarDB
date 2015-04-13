using System;
using System.Collections.Generic;

namespace BrightstarDB.EntityFramework.Tests.ContextObjects
{
    [Entity("Dinner")]
    public interface IDinner
    {
        [Identifier]
        string Id { get; }

        [PropertyType("dc:title")]
        string Title { get; set; }

        [PropertyType("host")]
        string Host { get; set; }

        [PropertyType("date")]
        DateTime EventDate { get; set; }

        [PropertyType("attendees")]
        ICollection<IRsvp> Rsvps { get; set; }

        [PropertyType("dinnerType")]
        DinnerType? DinnerType { get; set; }
    }

    public class Dinner : MockEntityObject, IDinner{
        #region Implementation of IDinner

        public string Id
        {
            get { throw new NotImplementedException(); }
        }

        public string Title
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string Host
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public DateTime EventDate
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ICollection<IRsvp> Rsvps
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public DinnerType? DinnerType
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}
