using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.Model
{
    /// <summary>
    /// Internal representation of a CURIE.
    /// </summary>
    internal class Curie
    {
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public bool IsValidCurie { get; private set; }

        public Curie(string curie)
        {
            curie = curie.Trim();
            if (curie.StartsWith("[") && curie.EndsWith("]"))
            {
                curie = curie.Substring(1, curie.Length - 2);
            }

            if (curie.IndexOf(":") > 0)
            {
                var split = curie.Split(':');
                if (split.Count() == 2)
                {
                    Prefix = split[0];
                    Suffix = split[1];
                    IsValidCurie = true;
                }
            }
        }
       
        public static Uri ResolveCurie(Curie c, Dictionary<string, string> mappings)
        {
            if (mappings.Keys.Contains(c.Prefix))
            {
                return new Uri(mappings[c.Prefix] + c.Suffix);
            }
            throw new BrightstarInternalException(String.Format("No mapping found for CURIE prefix '{0}'", c.Prefix));
        }
    }
}
