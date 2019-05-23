using System;
using System.Collections.Generic;
using System.Text;

namespace BrightstarDB.CodeGeneration
{
    public static class StringExtensions
    {
        public static string Pluralize(this string singular)
        {
            string root=singular, suffix;
            if (singular.EndsWith('y'))
            {
                root = singular.Substring(0, singular.Length - 1);
                suffix = "ies";
            }
            else if (singular.EndsWith("ch") || singular.EndsWith("sh"))
            {
                suffix = "es";
            }
            else
            {
                suffix = "s";
            }

            return root + suffix;
        }
    }
}
