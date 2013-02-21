using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework;

namespace BrightstarDB.Samples.NerdDinner.Models
{
    [Entity]
    public interface INerdDinnerLogin
    {
        [Identifier("http://nerddinner.com/logins/")]
        string Id { get; }
        string Username { get; set; }
        string Password { get; set; }
        string PasswordSalt { get; set; }
        string Email { get; set; }
        string Comments { get; set; }
        DateTime CreatedDate { get; set; }
        DateTime LastActive { get; set; }
        DateTime LastLoginDate { get; set; }
        bool IsActivated { get; set; }
        bool IsLockedOut { get; set; }
        DateTime LastLockedOutDate { get; set; }
        string LastLockedOutReason { get; set; }
        int? LoginAttempts { get; set; }
        ICollection<string> Roles { get; set; }
    }
}
