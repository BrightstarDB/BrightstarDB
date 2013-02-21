using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace BrightstarDB.SdShare
{
    public class NTripleLinePattern
    {
        private readonly string _pattern;

        public NTripleLinePattern(string pattern)
        {
            _pattern = pattern;
        }

        public List<string> Variables { get; set; }

        public bool IsLiteral { 
            get 
            { 
                return !_pattern.Trim().Split(' ')[2].Contains("{{");
            }
        }

        public void GenerateNTriples(StringBuilder output, DataRowAdaptor dataRow, List<string> excludeColumns, bool performEscape)
        {
            // replace normal variables
            var line = _pattern;
            foreach (var columnName in dataRow.ColumnNames)
            {
                var val = dataRow.GetValue(columnName);

                // need to make sure the value contains only valid XML chars
                // replace any non-valid xml characters.
                var xmlValidData = new StringBuilder();
                var chars = val.ToCharArray();
                foreach (var c in chars)
                {
                    xmlValidData.Append(GetValidXmlChar(c));
                }
                val = xmlValidData.ToString();

                var variablePosition = line.IndexOf("[[" + columnName + "]]");

                if (variablePosition >= 0 && string.IsNullOrEmpty(val))
                {
                    // if the variable exists but the value is null then skip
                    return;
                }
                else
                {
                    //replace value
                    if (IsValueLiteral("[[" + columnName + "]]"))
                    {
                        line = line.Replace("[[" + columnName + "]]", EscapeLiteral(val));
                    } else {
                        line = line.Replace("[[" + columnName + "]]", val);
                    }
                }                
            }
            
            // if contains COLNAME repeat for all 
            if (line.Contains("[[COLNAME]]") && line.Contains("[[VALUE]]"))
            {
                var gline = line;
                foreach (var columnName in dataRow.ColumnNames)
                {
                    // ignore excluded columns
                    if (excludeColumns!=null && excludeColumns.Contains(columnName)) continue;

                    var colVal = dataRow.GetValue(columnName);
                    if (string.IsNullOrEmpty(colVal))
                    {
                        continue;
                    }

                    var xmlValidData = new StringBuilder(); 
                    var chars = colVal.ToCharArray();
                    foreach (var c in chars)
                    {
                        xmlValidData.Append(GetValidXmlChar(c));
                    }
                    colVal = xmlValidData.ToString();

                    gline = gline.Replace("[[COLNAME]]", columnName);

                    if (IsValueLiteral("[[VALUE]]")) {
                        gline = gline.Replace("[[VALUE]]", EscapeLiteral(colVal.ToCharArray()));
                    } else {
                        gline = gline.Replace("[[VALUE]]", colVal);
                    }

                    // output
                    output.AppendLine(ApplyFunctions(gline, performEscape));

                    // reset gline
                    gline = line;
                }
            }
            else
            {
                // just add the line
                output.AppendLine(ApplyFunctions(line, performEscape));
            }           
        }

        private static char GetValidXmlChar(char c)
        {
            // Char	   ::=   	#x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
            var x9 = Int32.Parse("9", System.Globalization.NumberStyles.HexNumber);
            if (x9 == c) return c;

            var xA = Int32.Parse("A", System.Globalization.NumberStyles.HexNumber);
            if (xA == c) return c;

            var xD = Int32.Parse("D", System.Globalization.NumberStyles.HexNumber);
            if (xD == c) return c;

            // range check
            var x20 = Int32.Parse("20", System.Globalization.NumberStyles.HexNumber);
            var xD7FF = Int32.Parse("D7FF", System.Globalization.NumberStyles.HexNumber);
            if (c >= x20 && c <= xD7FF) return c;

            // range check
            var xE000 = Int32.Parse("E000", System.Globalization.NumberStyles.HexNumber);
            var xFFFD = Int32.Parse("FFFD", System.Globalization.NumberStyles.HexNumber);
            if (c >= xE000 && c <= xFFFD) return c;

            // range check
            var x10000 = Int32.Parse("10000", System.Globalization.NumberStyles.HexNumber);
            var x10FFFF = Int32.Parse("10FFFF", System.Globalization.NumberStyles.HexNumber);
            if (c >= x10000 && c <= x10FFFF) return c;

            return '?';
        }

        private bool IsValueLiteral(string pattern)
        {
            var literal = _pattern.Split(' ')[2];
            return (literal.Contains(pattern) && !literal.Contains("<") && !literal.Contains(">"));
        }

        private static string ApplyFunctions(string line, bool performEscape)
        {
            // we only have one function at the moment
            var fnNameStartPos = line.IndexOf("URLENCODE");
            if (fnNameStartPos >= 0)
            {
                // find closing bracket
                var closingBracktet = line.IndexOf(')', fnNameStartPos);

                // get param value
                var paramValue = line.Substring(fnNameStartPos + 10,
                                                 closingBracktet - (fnNameStartPos + 10));
                var encodedValue = Uri.EscapeDataString(paramValue);

                // execute function
                if (performEscape)
                {
                    encodedValue = encodedValue.Replace("%2B", "~~~2B~~~");
                    encodedValue = encodedValue.Replace("%2F", "~~~SLASH~~~");
                }

                // replace fn call with return value
                line = line.Replace("URLENCODE(" + paramValue + ")", encodedValue);
                return ApplyFunctions(line, performEscape);
            }
            
            return line;
        }

        private static string EscapeLiteral(IEnumerable<char> unescapedLiteral)
        {
            var line = new StringBuilder();
            char highSurrogate = '\ud800';

            foreach (var c in unescapedLiteral)
            {
                if (c == 0x20 || c == 0x21 || c >= 0x23 && c <= 0x5B || c >= 0x5D && c <= 0x7E)
                {
                    line.Append(c);
                }
                else switch (c)
                    {
                        case (char)0x09:
                            line.Append("\\t");
                            break;
                        case (char)0x0A:
                            line.Append("\\n");
                            break;
                        case (char)0x0D:
                            line.Append("\\r");
                            break;
                        case (char)0x22:
                            line.Append("\\\"");
                            break;
                        case (char)0x5C:
                            line.Append("\\\\");
                            break;
                        default:
                            if (char.IsHighSurrogate(c))
                            {
                                highSurrogate = c;
                            }
                            else if (char.IsLowSurrogate(c))
                            {
                                line.Append("\\U");
                                line.Append(Char.ConvertToUtf32(highSurrogate, c).ToString("X8"));
                            }
                            else if (c <= 0x8 || c == 0xB || c == 0xC || (c >= 0x0E && c <= 0x1F) ||
                                     (c > 0x7F && c <= 0xFFFF))
                            {
                                line.Append("\\u");
                                line.Append(((int)c).ToString("X4"));
                            }
                            break;
                    }
            }

            return line.ToString();
        }
    }
}
