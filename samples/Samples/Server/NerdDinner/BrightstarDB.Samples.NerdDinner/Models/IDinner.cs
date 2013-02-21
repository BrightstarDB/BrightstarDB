using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.NerdDinner.Models
{
    [Entity]
    public interface IDinner
    {
        [Identifier("http://nerddinner.com/dinners/")]
        string Id { get; }

        [Required(ErrorMessage = "Please provide a title for the dinner")]
        string Title { get; set; }

        string Description { get; set; }

        [Display(Name = "Event Date")]
        [DataType(DataType.DateTime)]
        DateTime EventDate { get; set; }

        [Required(ErrorMessage = "The event must have an address.")]
        string Address { get; set; }

        [Required(ErrorMessage = "Please enter the city where the event takes place")]
        string City { get; set; }

        [Required(ErrorMessage = "Please enter the name of the host of this event")]
        [Display(Name = "Host")]
        string HostedBy { get; set; }

        ICollection<IRSVP> RSVPs { get; set; }
    }
}
