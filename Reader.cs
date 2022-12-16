using System.Numerics;
using System.Text.RegularExpressions;
using static LispInterpreter.Types;

namespace LispInterpreter
{
    /// <summary>
    /// Reader that parses and tokenises Lisp S-expressions and atoms
    /// </summary>
    internal class Reader
    {
        public class ParseError : LispThrowable
        {
            public ParseError(string msg) : base(string.Concat("-- Eval Error! --\n", msg), ExceptionType.ERR) { }
        }

        List<string> tokens = new List<string>();
        int pos;

        public Reader(List<string> tokens)
        {
            this.tokens = tokens;
            pos = 0;
        }

        public string Next()
        {
            return tokens[pos++];
        }

        public string? Peek()
        {
            if (pos >= tokens.Count)
                return null;
            else
                return tokens[pos];
        }

        public static LispType ReadStr(string exp)
        {
            Reader reader = new Reader(Tokenize(exp));
            return reader.ReadForm();
        }

        public LispType ReadForm()
        {
            string? token = Peek();
            if (token == null) { throw new LispContinue(); }
            LispType form;
            switch (token)
            {
                // symbol
                case "'":
                    Next();
                    form = new LispList(new LispSymbol("quote"), ReadForm());
                    break;
                // quasiquote
                case "`":
                    Next();
                    form = new LispList(new LispSymbol("quasiquote"), ReadForm());
                    break;
                // unquote
                case ",":
                    Next();
                    form = new LispList(new LispSymbol("unquote"), ReadForm());
                    break;
                // unquote-splice
                case ",@":
                    Next();
                    form = new LispList(new LispSymbol("unquote-splice"), ReadForm());
                    break;
                // with-meta
                case "^":
                    Next();
                    form = new LispList(new LispSymbol("with-meta"), ReadForm());
                    break;
                // deref
                case "->":
                    Next();
                    form = new LispList(new LispSymbol("deref"), ReadForm());
                    break;
                // start of list
                case "(":
                    form = ReadList(new LispList(), '(', ')');
                    break;
                case ")":
                    throw new LispEvalException("unexpected close paren");
                default:
                    form = ReadAtom();
                    break;
            }
            return form;
        }

        public LispType ReadList(LispList lst, char start, char end)
        {
            string token = Next();
            if (token[0] != start)
            {
                throw new LispEvalException("expected '" + start + "'");
            }
            while ((token = Peek()) != null && token[0] != end)
            {
                lst.Add(ReadForm());
            }

            if (token == null)
            {
                throw new LispEvalException("failed to find close paren");
            }
            Next();
            return lst;
        }

        public LispType ReadAtom()
        {
            string token = Next();
            const string pattern = @"(^-?[0-9]+$)|([0#][xX][0-9a-fA-F]+)|([0#][bB][0-1]+)|(^-?[0-9][0-9.]*$)|(^null$)|(^#t$)|(^#f$)|(^""(?:[\\].|[^\\""])*""$)|(^"".*$)|:(.*)|(^[^""]*$)";
            Regex atom = new Regex(pattern);
            Match match = atom.Match(token);
            // Console.WriteLine("ReadAtom: token: ^" + token + "$");
            if (!match.Success)
            {
                throw new ParseError("unrecognized token: '" + token + "'");
            }
            // int
            if (match.Groups[1].Value != string.Empty)
            {
                return new LispNumber(BigInteger.Parse(match.Groups[1].Value));
            }
            // hex
            // TODO accepts invalid input like #xGHIJKL
            if (match.Groups[2].Value != string.Empty)
            {
                return new LispNumber(BigInteger.Parse(match.Groups[2].Value.Substring(2), System.Globalization.NumberStyles.HexNumber));
            }
            // binary
            else if (match.Groups[3].Value != string.Empty)
            {
                return new LispNumber(BigInteger.Parse(Convert.ToInt64(match.Groups[3].Value.Substring(2), 2).ToString()));
            }
            // float
            else if (match.Groups[4].Value != string.Empty)
            {
                return new LispFloat(float.Parse(match.Groups[4].Value));
            }
            // null
            else if (match.Groups[5].Value != string.Empty)
            {
                return Null;
            }
            // #t
            else if (match.Groups[6].Value != string.Empty)
            {
                return True;
            }
            // #f
            else if (match.Groups[7].Value != string.Empty)
            {
                return False;
            }
            // string
            else if (match.Groups[8].Value != string.Empty)
            {
                string str = match.Groups[8].Value;
                str = str.Substring(1, str.Length - 2)
                    .Replace("\\\\", "\u029e")
                    .Replace("\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\u029e", "\\");
                return new LispString(str);
            }
            else if (match.Groups[9].Value != string.Empty)
            {
                throw new ParseError("expected '\"', got EOF");
            }
            else if (match.Groups[10].Value != string.Empty)
            {
                return new LispString("\u029e" + match.Groups[10].Value);
            }
            else if (match.Groups[11].Value != string.Empty)
            {
                return new LispSymbol(match.Groups[11].Value);
            }
            else
            {
                throw new ParseError("unrecognized atom: '" + match.Groups[0] + "'");
            }
        }

        public static List<string> Tokenize(string exp)
        {
            const string pattern = @"[\s ]*(,@|[\[\]{}()'`,@]|""(?:[\\].|[^\\""])*""?|;.*|[^\s \[\]{}()'""`~@,;]*)";
            Regex sexp = new Regex(pattern);
            List<string> tokens = new List<string>();
            foreach (Match match in sexp.Matches(exp))
            {
                string token = match.Groups[1].Value;
                if (token != null && !(token == string.Empty) && !(token[0] == ';'))
                    tokens.Add(token);
            }
            // Console.WriteLine(String.Join(", ", tokens));
            return tokens;
        }
    }
}
