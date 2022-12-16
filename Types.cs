using System.Numerics;

namespace LispInterpreter
{
    /// <summary>
    /// Lisp types and exceptions
    /// </summary>
    public class Types
    {
        public enum ExceptionType
        {
            WARN,
            ERR
        }

        /// <summary>
        /// basic exception for use with the Reader/Printer
        /// </summary>
        public class LispThrowable : Exception
        {
            ExceptionType exceptionType;
            public LispThrowable() : base() { }
            public LispThrowable(string msg) : base(msg) { }
            public LispThrowable(string msg, ExceptionType type) : base(msg) { exceptionType = type; }

            bool IsError()
            {
                return exceptionType == ExceptionType.ERR;
            }
        }

        public class LispException : LispThrowable
        {
            LispType value;
            public LispException(LispType value) : base(Printer.PrintStr(value), ExceptionType.ERR)
            {
                this.value = value;                
            }
            public LispException(string msg) : base(msg, ExceptionType.ERR)
            {
                this.value = new LispString(msg);
            }

            public LispType GetValue()
            {
                return value;
            }
        }

        public class LispEvalException : LispException
        {
            public LispEvalException(string msg) : base(string.Concat("-- Eval Error! --\n", msg)) { }
            public LispEvalException(string msg, LispType ast) : base(string.Concat(string.Concat("-- Eval Error! --\n", msg), "\nForm:\n" + Printer.PrintStr(ast, true))) { }
        }

        public class LispContinue : LispThrowable { }

        public static bool Equals(LispType a, LispType b)
        {
            Type ota = a.GetType(), otb = b.GetType();
            if (!((ota == otb) ||
                (a is LispList && b is LispList)))
            {
                return false;
            }
            else
            {
                if (a is LispNumber)
                {
                    return ((LispNumber)a).GetValue() == ((LispNumber)b).GetValue();
                }
                else if (a is LispSymbol)
                {
                    return ((LispSymbol)a).GetName() == ((LispSymbol)b).GetName();
                }
                else if (a is LispString)
                {
                    return ((LispString)a).GetValue() == ((LispString)b).GetValue();
                }
                else if (a is LispList)
                {
                    if (((LispList)a).Size() != ((LispList)b).Size())
                    {
                        return false;
                    }
                    for (int i = 0; i < ((LispList)a).Size(); i++)
                    {
                        if (!Equals(((LispList)a)[i], ((LispList)b)[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else if (a is LispHashMap)
                {
                    var akeys = ((LispHashMap)a).GetValue().Keys;
                    var bkeys = ((LispHashMap)b).GetValue().Keys;
                    if (akeys.Count != bkeys.Count)
                    {
                        return false;
                    }
                    foreach (var k in akeys)
                    {
                        if (!Equals(((LispHashMap)a).GetValue()[k], ((LispHashMap)b).GetValue()[k]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return a == b;
                }
            }
        }

        /// <summary>
        /// base class that every other type inherits from.
        /// </summary>
        public abstract class LispType
        {
            LispType meta = Null;
            public virtual LispType Copy()
            {
                return (LispType)MemberwiseClone();
            }
            public virtual string? ToString(bool value_only)
            {
                return ToString();
            }
            public LispType GetMeta() { return meta; }
            public LispType SetMeta(LispType val) { meta = val; return this; }
            public virtual bool IsList() { return false; }
        }

        /// <summary>
        /// a constant
        /// </summary>
        public class LispConst : LispType
        {
            string value;
            public LispConst(string value)
            {
                this.value = value;
            }
            public override string ToString()
            {
                return value;
            }
            public override string ToString(bool value_only)
            {
                return value;
            }
        }

        /// <summary>
        /// a symbol, basically anything that is not any of the other types.
        /// </summary>
        public class LispSymbol : LispType
        {
            string value;
            public LispSymbol(string value) { this.value = value; }
            public LispSymbol(LispString str) { value = str.GetValue(); }
            public string GetName() { return value; }
            public override int GetHashCode()
            {
                return value.GetHashCode();
            }
            public override string ToString()
            {
                return "symbol\nname: " + value + "\nhash: " + GetHashCode() + "\n";
            }
            public override string ToString(bool value_only)
            {
                return value;
            }
        }

        /// <summary>
        /// a Lisp atom
        /// </summary>
        public class LispAtom : LispType
        {
            LispType value;
            public LispAtom(LispType value)
            {
                this.value = value;
            }
            public LispType GetValue() { return value; }
            public LispType SetValue(LispType value) { return this.value = value; }
            public override string ToString()
            {
                return "(atom " + Printer.PrintStr(value, true) + ")";
            }
            public override string ToString(Boolean value_only)
            {
                return "(atom " + Printer.PrintStr(value, value_only) + ")";
            }
        }

        /// <summary>
        /// base class for number values
        /// </summary>
        public class LispNumber : LispType
        {
            BigInteger value;
            public LispNumber() { value = 0; }
            public LispNumber(BigInteger value) { this.value = value; }
            public BigInteger GetValue() { return value; }
            public override string ToString()
            {
                return "int\n" + value.ToString() + "\t\t" + Printer.AsHex(this);
            }
            public override string ToString(bool value_only)
            {
                return value.ToString();
            }
            public virtual LispNumber Add(LispNumber b)
            {
                return new LispNumber(GetValue() + b.GetValue());
            }
            public virtual LispNumber Subtract(LispNumber b)
            {
                return new LispNumber(GetValue() - b.GetValue());
            }
            public virtual LispNumber Multiply(LispNumber b)
            {
                return new LispNumber(GetValue() * b.GetValue());
            }
            public virtual LispNumber Divide(LispNumber b)
            {
                return new LispNumber(GetValue() / b.GetValue());
            }
            public virtual LispConst LessThan(LispNumber b)
            {
                return GetValue() < b.GetValue() ? True : False;
            }
            public virtual LispConst GreaterThan(LispNumber b)
            {
                return GetValue() > b.GetValue() ? True : False;
            }
            public virtual LispConst LessEquals(LispNumber b)
            {
                return GetValue() <= b.GetValue() ? True : False;
            }
            public virtual LispConst GreaterEquals(LispNumber b)
            {
                return GetValue() >= b.GetValue() ? True : False;
            }

            public static LispConst operator <(LispNumber a, LispNumber b)
            {
                return a.LessThan(b);
            }
            public static LispConst operator >(LispNumber a, LispNumber b)
            {
                return a.GreaterThan(b);
            }
            public static LispConst operator <=(LispNumber a, LispNumber b)
            {
                return a.LessEquals(b);
            }
            public static LispConst operator >=(LispNumber a, LispNumber b)
            {
                return a.GreaterEquals(b);
            }
            public static LispNumber operator +(LispNumber a, LispNumber b)
            {
                return a.Add(b);
            }
            public static LispNumber operator -(LispNumber a, LispNumber b)
            {
                return a.Subtract(b);
            }
            public static LispNumber operator *(LispNumber a, LispNumber b)
            {
                return a.Multiply(b);
            }
            public static LispNumber operator /(LispNumber a, LispNumber b)
            {
                return a.Divide(b);
            }
            public static LispNumber operator ++(LispNumber a)
            {
                return new LispNumber(a.GetValue() + 1);
            }
        }

        /// <summary>
        /// a floating point number
        /// </summary>
        public class LispFloat : LispNumber
        {
            float value;
            public LispFloat(float value)
            {
                this.value = value;
            }
            public new float GetValue() { return value; }
            public override string ToString()
            {
                return "float\n" + value.ToString() + "\t\t" + Printer.AsHex(this);
            }
            public override string ToString(bool value_only)
            {
                return value.ToString();
            }
            public override LispNumber Add(LispNumber b)
            {
                return new LispFloat(GetValue() + ((LispFloat)b).GetValue());
            }
            public override LispNumber Subtract(LispNumber b)
            {
                return new LispFloat(GetValue() - ((LispFloat)b).GetValue());
            }
            public override LispNumber Multiply(LispNumber b)
            {
                return new LispFloat(GetValue() * ((LispFloat)b).GetValue());
            }
            public override LispNumber Divide(LispNumber b)
            {
                return new LispFloat(GetValue() / ((LispFloat)b).GetValue());
            }
            public override LispConst LessThan(LispNumber b)
            {
                return GetValue() < ((LispFloat)b).GetValue() ? True : False;
            }
            public override LispConst GreaterThan(LispNumber b)
            {
                return GetValue() > ((LispFloat)b).GetValue() ? True : False;
            }
            public override LispConst LessEquals(LispNumber b)
            {
                return GetValue() <= ((LispFloat)b).GetValue() ? True : False;
            }
            public override LispConst GreaterEquals(LispNumber b)
            {
                return GetValue() >= ((LispFloat)b).GetValue() ? True : False;
            }
        }

        /// <summary>
        /// a string, value in quotes ""
        /// </summary>
        public class LispString : LispType
        {
            string value;
            public LispString(string value) { this.value = value; }
            public string GetValue() { return value; }
            public override string ToString()
            {
                return "string\n" + "\"" + value + "\"";
            }
            public override string ToString(bool value_only)
            {
                if (value.Length > 0 && value[0] == '\u029e')
                {
                    return ":" + value.Substring(1);
                }
                else if (value_only)
                {
                    return "\"" + value.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n") + "\"";
                }
                else
                {
                    return value;
                }
            }
        }

        /// <summary>
        /// a list, holds a number of Lisp values
        /// </summary>
        public class LispList : LispType
        {
            public string start = "(";
            public string end = ")";
            List<LispType> value;
            public LispList() { value = new List<LispType>(); }
            public LispList(List<LispType> lst) { value = lst; }
            public LispList(params LispType[] vals)
            {
                value = new List<LispType>(vals);
            }
            public List<LispType> GetValue() { return value; }
            public int Size() { return value.Count; }
            public LispList Add(params LispType[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    value.Add(values[i]);
                }
                return this;
            }
            public LispList conj_BANG(params LispType[] mvs)
            {
                for (int i = 0; i < mvs.Length; i++)
                {
                    value.Add(mvs[i]);
                }
                return this;
            }
            public LispType nth(int idx)
            {
                return value.Count > idx ? value[idx] : Null;
            }
            public LispType this[int idx] { get { return value.Count > idx ? value[idx] : Null; } }
            public LispList Rest()
            {
                if (Size() > 0)
                {
                    return new LispList(value.GetRange(1, value.Count - 1));
                }
                else
                {
                    return new LispList();
                }
            }
            public virtual LispList Slice(int start)
            {
                return new LispList(value.GetRange(start, value.Count - start));
            }
            public virtual LispList Slice(int start, int end)
            {
                return new LispList(value.GetRange(start, end - start));
            }
            public override string ToString()
            {
                return start + Printer.Join(value, " ", true) + end;
            }
            public override string ToString(bool value_only)
            {
                return start + Printer.Join(value, " ", value_only) + end;
            }
            public override bool IsList() { return true; }
        }

        /// <summary>
        /// vector type
        /// </summary>
        public class LispVector : LispList
        {
            // Same implementation except for instantiation methods
            public LispVector() : base()
            {
                start = "[";
                end = "]";
            }
            public LispVector(List<LispType> val) : base(val)
            {
                start = "[";
                end = "]";
            }

            public override bool IsList() { return false; }

            public override LispList Slice(int start, int end)
            {
                var val = GetValue();
                return new LispVector(val.GetRange(start, val.Count - start));
            }
        }

        /// <summary>
        /// hashmap type
        /// </summary>
        public class LispHashMap : LispType
        {
            Dictionary<string, LispType> value;
            public LispHashMap(Dictionary<string, LispType> val)
            {
                value = val;
            }
            public LispHashMap(LispList lst)
            {
                value = new Dictionary<string, LispType>();
                assoc_BANG(lst);
            }
            public new LispHashMap Copy()
            {
                var new_self = (LispHashMap)MemberwiseClone();
                new_self.value = new Dictionary<string, LispType>(value);
                return new_self;
            }

            public Dictionary<string, LispType> GetValue() { return value; }

            public override string ToString()
            {
                return "{" + Printer.Join(value, " ", true) + "}";
            }
            public override string ToString(bool print_readably)
            {
                return "{" + Printer.Join(value, " ", print_readably) + "}";
            }

            public LispHashMap assoc_BANG(LispList lst)
            {
                for (int i = 0; i < lst.Size(); i += 2)
                {
                    value[((LispString)lst[i]).GetValue()] = lst[i + 1];
                }
                return this;
            }

            public LispHashMap dissoc_BANG(LispList lst)
            {
                for (int i = 0; i < lst.Size(); i++)
                {
                    value.Remove(((LispString)lst[i]).GetValue());
                }
                return this;
            }
        }

        /// <summary>
        /// a lisp function.
        /// </summary>
        public class LispFunc : LispType
        {
            Func<LispList, LispType> func;
            LispType? ast;
            Env? env;
            LispList? fnParams;
            bool macro = false;
            // string name;
            public LispFunc(Func<LispList, LispType> func) { this.func = func; }
            public LispFunc(Func<LispList, LispType> func, LispType ast, Env env, LispList fnParams)
            {
                this.func = func;
                this.ast = ast;
                this.env = env;
                this.fnParams = fnParams;
            }

            public override string ToString()
            {
                if (ast != null)
                {
                    return "#<function " + Printer.PrintStr(fnParams, true) + " " + Printer.PrintStr(ast, true) + ">";
                }
                else
                {
                    return "#<builtin function " + func.Method.Name + /*ToString()*/ ">";
                }
            }
            public LispType Apply(LispList args)
            {
                return func(args);
            }

            public LispType? GetAst()
            {
                return ast;
            }

            public Env? GetEnv()
            {
                return env;
            }

            public Env? GenEnv(LispList args)
            {
                return new Env(env, fnParams, args);
            }

            public bool IsMacro() { return macro; }
            public void SetMacro(bool val) { macro = val; }
        }

        /// constants
        public static LispConst Null = new LispConst("null");
        public static LispConst True = new LispConst("#t");
        public static LispConst False = new LispConst("#f");
    }
}
