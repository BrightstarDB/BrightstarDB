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


        public override bool Matches(object actual)
        {
            base.actual = actual;
            if (actual is string)
            {
                return _expectedRegex.IsMatch(actual as string);
            }
            return false;
        }

        public override void WriteDescriptionTo(MessageWriter writer)
        {
            writer.Write("Link header containing link with ");
            writer.WriteExpectedValue("rel=" + _expectedRel + " and location=<"+_expectedUri+ ">");
        }
    }
}
