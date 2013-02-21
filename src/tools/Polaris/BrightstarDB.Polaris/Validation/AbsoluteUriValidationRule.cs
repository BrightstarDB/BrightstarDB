using System;
using System.Globalization;
using System.Windows.Controls;

namespace BrightstarDB.Polaris.Validation
{
    public class AbsoluteUriValidationRule : ValidationRule
    {
        #region Overrides of ValidationRule

        /// <summary>
        /// When overridden in a derived class, performs validation checks on a value.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Windows.Controls.ValidationResult"/> object.
        /// </returns>
        /// <param name="value">The value from the binding target to check.</param><param name="cultureInfo">The culture to use in this rule.</param>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var szValue = (string) value;
            if (!String.IsNullOrEmpty(szValue))
            {
                Uri u;
                if (!Uri.TryCreate(szValue, UriKind.Absolute, out u))
                {
                    return new ValidationResult(false, "Value must be a valid absolute URI");
                }
            }
            return new ValidationResult(true, null);
        }

        #endregion
    }
}
