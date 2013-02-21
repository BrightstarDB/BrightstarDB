using System.ComponentModel.DataAnnotations;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.NerdDinner.Models
{
    [Entity]
    public interface IRSVP
    {
        [Identifier("http://nerddinner.com/rsvps/")]
        string Id { get; }

        [Display(Name = "Email Address")]
        [Required(ErrorMessage = "Email address is required")]
        string AttendeeEmail { get; set; }

        [InverseProperty("RSVPs")]
        IDinner Dinner { get; set; }
    }
}
