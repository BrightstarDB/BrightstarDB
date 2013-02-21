/**
 * MIME-Type Parser
 * 
 * This class provides basic functions for handling mime-types. It can handle
 * matching mime-types against a list of media-ranges. See section 14.1 of the
 * HTTP specification [RFC 2616] for a complete explanation.
 * 
 * http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.1
 * 
 * A port to C# of Joe Gregorio's MIME-Type Parser:
 * 
 * http://code.google.com/p/mimeparse/
 * 
 * Ported by Kal Ahmed <kal@brightstardb.com>
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MimeParse
{
    public class MimeParse
    {
        /// <summary>
        /// Parse Results container
        /// </summary>
        public class ParseResults
        {
            public String Type { get; set; }

            public String SubType { get; set; }

            // A dictionary of all the parameters for the media range
            public Dictionary<String, String> Parameters { get; set; }

            public override String ToString()
            {
                var s = new StringBuilder();
                s.AppendFormat("('{0}', '{1}'", Type, SubType);
                s.Append(", {");
                foreach (String k in Parameters.Keys.OrderBy(k => k))
                {
                    s.AppendFormat("'{0}':'{1}',", k, Parameters[k]);
                }
                s.Append("})");
                return s.ToString();
            }
        }

        /// <summary>
        /// Carves up a mime-type and returns a ParseResults object
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        /// <remarks>For example, the media range 'application/xhtml;q=0.5' would get parsed into:
        /// ('application', 'xhtml', {'q', '0.5'})</remarks>
        public static ParseResults ParseMimeType(String mimeType)
        {
            String[] parts = mimeType.Split(';');
            ParseResults results = new ParseResults {Parameters = new Dictionary<String, String>()};

            for (int i = 1; i < parts.Length; i++)
            {
                String p = parts[i];
                String[] subParts = p.Split('=');
                if (subParts.Length == 2)
                {
                    results.Parameters[subParts[0].Trim()] = subParts[1].Trim();
                }
            }
            String fullType = parts[0].Trim();

            // Java URLConnection class sends an Accept header that includes a
            // single "*" - Turn it into a legal wildcard.
            if (fullType.Equals("*"))
                fullType = "*/*";
            String[] types = fullType.Split('/');
            results.Type = types[0].Trim();
            results.SubType = types[1].Trim();
            return results;
        }

        /// <summary>
        /// Carves up a media range and returns a ParseResults.
        /// </summary>
        /// <remarks>
        /// For example, the media range 'application/*;q=0.5' would get parsed into:
        /// ('application', '*', {'q', '0.5'})
        /// In addition this function also guarantees that there is a value for 'q'
        /// in the params dictionary, filling it in with a proper default if
        /// necessary.
        /// </remarks>
        public static ParseResults ParseMediaRange(String range)
        {
            ParseResults results = ParseMimeType(range);
            if (!results.Parameters.ContainsKey("q") || String.IsNullOrEmpty(results.Parameters["q"]))
            {
                results.Parameters["q"] = "1";
            }
            String q = results.Parameters["q"];
            float f = float.Parse(q);
            if (f < 0 || f > 1)
            {
                results.Parameters["q"] = "1";
            }
            return results;
        }

        /// <summary>Structure for holding a fitness/quality combo</summary>
        public class FitnessAndQuality : IComparable<FitnessAndQuality>
        {
            private readonly int _fitness;

            private readonly float _quality;

            public float Quality
            {
                get { return _quality; }
            }

            public string MimeType { get; set; }

            public FitnessAndQuality(int fitness, float quality)
            {
                this._fitness = fitness;
                this._quality = quality;
            }

            #region Implementation of IComparable<in FitnessAndQuality>

            /// <summary>
            /// Compares the current object with another object of the same type.
            /// </summary>
            /// <returns>
            /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
            /// </returns>
            /// <param name="other">An object to compare with this object.</param>
            public int CompareTo(FitnessAndQuality other)
            {
                if (_fitness == other._fitness)
                {
                    if (Math.Abs(_quality - other._quality) < 0.05)
                        return 0;
                    return _quality < other._quality ? -1 : 1;
                }
                return _fitness < other._fitness ? -1 : 1;
            }

            #endregion
        }

        /// <summary>
        /// Find the best match for a given mimeType against a list of media_ranges
        /// that have already been parsed by MimeParse.parseMediaRange(). 
        /// </summary>
        /// <remarks>Returns a
        /// tuple of the fitness value and the value of the 'q' quality parameter of
        /// the best match, or (-1, 0) if no match was found. Just as for
        /// quality_parsed(), 'parsed_ranges' must be a list of parsed media ranges.
        /// </remarks>
        public static FitnessAndQuality FitnessAndQualityParsed(String mimeType, ICollection<ParseResults> parsedRanges)
        {
            int bestFitness = -1;
            float bestFitQ = 0;
            ParseResults target = ParseMediaRange(mimeType);

            foreach (ParseResults range in parsedRanges)
            {
                if ((target.Type.Equals(range.Type) || range.Type.Equals("*") || target.Type.Equals("*"))
                    && (target.SubType.Equals(range.SubType) || range.SubType.Equals("*") || target.SubType.Equals("*")))
                {
                    foreach (String k in target.Parameters.Keys)
                    {
                        int paramMatches = 0;
                        if (!k.Equals("q") && range.Parameters.ContainsKey(k)
                            && target.Parameters[k].Equals(range.Parameters[k]))
                        {
                            paramMatches++;
                        }
                        int fitness = (range.Type.Equals(target.Type)) ? 100 : 0;
                        fitness += (range.SubType.Equals(target.SubType)) ? 10 : 0;
                        fitness += paramMatches;
                        if (fitness > bestFitness)
                        {
                            bestFitness = fitness;
                            bestFitQ = range.Parameters.ContainsKey("q") ? float.Parse(range.Parameters["q"]) : 0;
                        }
                    }
                }
            }
            return new FitnessAndQuality(bestFitness, bestFitQ);
        }


        /// <summary>
        /// Find the best match for a given mime-type against a list of ranges that
        ///  have already been parsed by <see cref="ParseMediaRange(string)"/>.
        /// </summary>
        /// <remarks>
        /// Returns the 'q' quality
        ///  parameter of the best match, 0 if no match was found. This function
        ///  behaves the same as <see cref="Quality(string,string)"/> except that 'parsed_ranges' must be a list
        ///  of parsed media ranges.
        /// </remarks>
        /// <param name="mimeType"></param>
        /// <param name="parsedRanges"></param>
        /// <returns></returns>
        protected static float QualityParsed(String mimeType, ICollection<ParseResults> parsedRanges)
        {
            return FitnessAndQualityParsed(mimeType, parsedRanges).Quality;
        }

        ///<summary>
        /// Returns the quality 'q' of a mime-type when compared against the
        /// mediaRanges in ranges. 
        /// </summary>
        public static float Quality(String mimeType, String ranges)
        {
            var results = ranges.Split(',').Select(ParseMediaRange).ToList();
            return QualityParsed(mimeType, results);
        }

        ///<summary>
        /// Takes a list of supported mime-types and finds the best match for all the
        /// media-ranges listed in header. 
        /// </summary>
        /// <remarks>
        /// The value of header must be a string that
        /// conforms to the format of the HTTP Accept: header. The value of
        /// 'supported' is a list of mime-types.
        /// </remarks>
        /// <example><code>MimeParse.MimeParse.BestMatch(new [] { "application/xbel+xml", "text/xml" }, "text/*;q=0.5,*; q=0.1");// Returns "text/xml"</code></example>
        public static String BestMatch(ICollection<String> supported, String header)
        {
            var weightedMatches = new List<FitnessAndQuality>();
            var parseResults = header.Split(',').Select(ParseMediaRange).ToList();

            foreach (String s in supported)
            {
                FitnessAndQuality fitnessAndQuality = FitnessAndQualityParsed(s,
                                                                              parseResults);
                fitnessAndQuality.MimeType = s;
                weightedMatches.Add(fitnessAndQuality);
            }
            weightedMatches.Sort();

            FitnessAndQuality lastOne = weightedMatches.Last();
            return lastOne.Quality > 0 ? lastOne.MimeType : "";
        }

        // hidden
        private MimeParse()
        {
        }
    }
}