using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace BrightstarDB.Polaris.Validation
{
    public class RegexValidationRule : ValidationRule
    {
        public string Expression { get; set; }
        public string Message { get; set; }

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
            if (!Regex.IsMatch((string)value, Expression))
            {
                return new ValidationResult(false, Message);
            }
            return new ValidationResult(true, null);
        }

        #endregion
    }
}
