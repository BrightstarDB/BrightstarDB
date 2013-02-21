using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BrightstarDB.Azure.Gateway.Models
{
    public class AccountRegistration : IValidatableObject
    {
        [Required]
        [Display(Name="Contact Email Address",
            Description = "We will ONLY use this email address to contact you about our services and your account. Your email address will not be used for any other purpose.")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name="Confirm Email Address",
            Description = "Please re-enter your email address to confirm spelling.")]
        public string ConfirmEmailAddress { get; set; }

        #region Implementation of IValidatableObject

        /// <summary>
        /// Determines whether the specified object is valid.
        /// </summary>
        /// <returns>
        /// A collection that holds failed-validation information.
        /// </returns>
        /// <param name="validationContext">The validation context.</param>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(EmailAddress))
            {
                yield return new ValidationResult("A contact email address is REQUIRED", new []{ "EmailAddress"});
            }
            if (string.IsNullOrEmpty(ConfirmEmailAddress))
            {
                yield return new ValidationResult("Please enter your email address twice to confirm the spelling", new[] { "ConfirmEmailAddress" });
            }
            if (!string.IsNullOrEmpty(EmailAddress) && !string.IsNullOrEmpty(ConfirmEmailAddress) && !EmailAddress.Equals(ConfirmEmailAddress))
            {
                yield return new ValidationResult("Email addresses do not match.", new[] {"EmailAddress", "ConfirmEmailAddress"});
            }
        }

        #endregion
    }
}