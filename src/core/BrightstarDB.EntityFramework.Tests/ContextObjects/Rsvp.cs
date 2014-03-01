namespace BrightstarDB.EntityFramework.Tests.ContextObjects
{
    [Entity("Rsvp")]
    public interface IRsvp
    {
        [Identifier]
        string Id { get; }

        [PropertyType("email")]
        string AttendeeEmail { get; set; }

        [InversePropertyType("attendees")]
        IDinner Dinner { get; set; }
    }

    public class Rsvp : MockEntityObject, IRsvp
    {

        #region Implementation of IRsvp

        public string Id
        {
            get { throw new System.NotImplementedException(); }
        }

        public string AttendeeEmail
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public IDinner Dinner
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        #endregion
    }
}