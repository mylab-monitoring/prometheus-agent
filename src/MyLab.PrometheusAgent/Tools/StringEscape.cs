using System;
using System.Linq;
using System.Text;

namespace MyLab.PrometheusAgent.Tools
{
    static class StringEscape
    {
        public static string Escape(string input)
        {
            var literal = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    case '\'': literal.Append(@"\'"); break;
                    default: literal.Append(c); break;
                }
            }
            return literal.ToString();
        }

        public static string Unescape(string escaped)
        {
            bool prevSlash = false;

            var literal = new StringBuilder(escaped.Length);

            for (int i = 0; i < escaped.Length; i++)
            {
                var c = escaped[i];

                if (c == '\\')
                {
                    if (prevSlash)
                    {
                        literal.Append("\\");

                        prevSlash = false;
                        continue;
                    }
                    else
                    {
                        prevSlash = true;

                        continue;
                    }
                }

                if (prevSlash)
                {
                    switch (c)
                    {
                        case '\"': literal.Append("\""); break;
                        case '\\': literal.Append("\\"); break;
                        case '0': literal.Append("\0");  break;
                        case 'a': literal.Append("\a");  break;
                        case 'b': literal.Append("\b");  break;
                        case 'f': literal.Append("\f");  break;
                        case 'n': literal.Append("\n");  break;
                        case 'r': literal.Append("\r");  break;
                        case 't': literal.Append("\t");  break;
                        case 'v': literal.Append("\v");  break;
                        case '\'': literal.Append("\'"); break;
                        default:
                        {
                            literal.Append("\\");
                            literal.Append(c);
                        }
                            break;
                    }

                    prevSlash = false;
                    continue;
                }

                literal.Append(c);
            }

            return literal.ToString();
        }
    }
}
