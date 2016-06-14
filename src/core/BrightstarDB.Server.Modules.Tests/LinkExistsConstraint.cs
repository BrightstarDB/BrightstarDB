using System.Text.RegularExpressions;
using NUnit.Framework.Constraints;

namespace BrightstarDB.Server.Modules.Tests
{
    class LinkExistsConstraint : Constraint
    {
        private readonly string _expectedRel;
        private readonly string _expectedUri;
        private readonly Regex _expectedRegex;

        public LinkExistsConstraint(string rel, string uri) 
        {
            _expectedRel = rel;
            _expectedUri = uri;
            _expectedRegex = new Regex("<" + Regex.Escape(uri) + @">\s*;\s*rel\s*=\s*" + Regex.Escape(rel));
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual is string)
            {
                var isMatch = _expectedRegex.IsMatch(actual as string);
                if (isMatch) return new ConstraintResult(this, actual, ConstraintStatus.Success);
            }
            var result = new ConstraintResult(this, actual, ConstraintStatus.Failure);
            return result;
        }

        public override string Description => "Link header containing link with rel=" + _expectedRel + " and location=<" + _expectedUri + ">";

    }

    
}
