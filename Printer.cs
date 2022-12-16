using System.Text.RegularExpressions;
using static LispInterpreter.Types;

namespace LispInterpreter
{
    /// <summary>
    /// utility class for printing string representations of Lisp forms
    /// </summary>
    internal class Printer
    {
        public static string? PrintStr(LispType? type)
        {
            return type.ToString();
        }

        public static string? PrintStr(LispType? val, bool value_only)
        {
            return val.ToString(value_only);
        }

        public static string? PrintFn(LispFunc fn, bool value_only)
        {
            return fn.ToString();
        }

        public static string Join(List<LispType> value,
                        string delim, bool value_only)
        {
            List<string> vals = new List<string>();
            foreach (LispType val in value)
            {
                vals.Add(val.ToString(value_only));
            }
            return string.Join(delim, vals.ToArray());
        }

        public static string Join(Dictionary<string, LispType> value,
                                string delim, bool value_only)
        {
            List<string> strs = new List<string>();
            foreach (KeyValuePair<string, LispType> entry in value)
            {
                if (entry.Key.Length > 0 && entry.Key[0] == '\u029e')
                {
                    strs.Add(":" + entry.Key.Substring(1));
                }
                else if (value_only)
                {
                    strs.Add("\"" + entry.Key.ToString() + "\"");
                }
                else
                {
                    strs.Add(entry.Key.ToString());
                }
                strs.Add(entry.Value.ToString(value_only));
            }
            return string.Join(delim, strs.ToArray());
        }

        public static string Format(LispList args, string sep,
                                  bool print_readably)
        {
            return Join(args.GetValue(), sep, print_readably);
        }

        public static string EscapeString(string str)
        {
            return Regex.Escape(str);
        }

        public static string AsHex(LispType type)
        {
            switch (type)
            {
                case LispNumber:
                    try
                    {
                        return string.Format("#x{0:x}", ((LispNumber)type).GetValue());
                    }
                    catch (FormatException)
                    {
                        return "#<error>";
                    }
                case LispString:
                    try
                    {
                        return string.Format("#x{0:x}", ((LispString)type).GetValue());
                    }
                    catch (FormatException)
                    {
                        return "#<error>";
                    }
                default:
                    return "";
            }
        }

        public static string AsBinary(LispType type) {
            switch (type) {
                case LispNumber:
                    try {
                        return "#b" + Convert.ToString((int)((LispNumber)type).GetValue(), 2);
                    }
                    catch (FormatException) {
                        return "#<error>";
                    }
                default:
                    return "";
            }
        }
    }
}
