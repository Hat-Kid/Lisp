using static LispInterpreter.Types;

namespace LispInterpreter
{
    /// <summary>
    /// defines the core Lisp functions
    /// </summary>
    public class Core
    {
        static public LispFunc _throw = new LispFunc(args => throw new LispException(args[0]));

        static LispFunc null_Q = new LispFunc(args => args[0] == Null ? True : False);
        static LispFunc true_Q = new LispFunc(args => args[0] == True ? True : False);
        static LispFunc false_Q = new LispFunc(args => args[0] == False ? True : False);
        static LispFunc symbol_Q = new LispFunc(a => a[0] is LispSymbol ? True : False);
        static LispFunc number_Q = new LispFunc(a => a[0] is LispNumber ? True : False);
        static LispFunc function_Q = new LispFunc(a => a[0] is LispFunc && !((LispFunc)a[0]).IsMacro() ? True : False);
        static LispFunc macro_Q = new LispFunc(a => a[0] is LispFunc && ((LispFunc)a[0]).IsMacro() ? True : False);
        static LispFunc string_Q = new LispFunc(args =>
        {
            if (args[0] is LispString)
            {
                var s = ((LispString)args[0]).GetValue();
                return (s.Length == 0 || s[0] != '\u029e') ? True : False;
            }
            else
            {
                return False;
            }
        });

        static LispFunc keyword = new LispFunc(args =>
        {
            if (args[0] is LispString &&
                ((LispString)args[0]).GetValue()[0] == '\u029e')
            {
                return args[0];
            }
            else
            {
                return new LispString("\u029e" + ((LispString)args[0]).GetValue());
            }
        });

        static LispFunc keyword_Q = new LispFunc(a =>
        {
            if (a[0] is LispString)
            {
                var s = ((LispString)a[0]).GetValue();
                return (s.Length > 0 && s[0] == '\u029e') ? True : False;
            }
            else
            {
                return False;
            }
        });

        // number functions
        static LispFunc time_ms = new LispFunc(args => new LispNumber(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));

        // string functions
        static public LispFunc format_str = new LispFunc(args => new LispString(Printer.Format(args, " ", true)));
        static public LispFunc str = new LispFunc(args => new LispString(Printer.Format(args, "", false)));

        // formats a string akin to printf with escape codes
        static public LispFunc format = new LispFunc(args =>
        {
            try {
                string fmt_str = ((LispString)args[0]).GetValue();
                int arg_count = args.Slice(1).Size();
                int cmd_pos = 1;
                List<char> buf = new List<char>();
                for (int i = 0; i < fmt_str.Length; i++) {
                    char fmt_char = fmt_str[i];
                    buf.Add(fmt_char);
                    if (fmt_char == '~') {
                        char fmt_cmd = fmt_str[i + 1];
                        buf.RemoveAt(buf.Count - 1); // remove tilde from char buffer
                        switch (fmt_cmd) {
                            // escape tilde
                            case '~':
                                buf.Add('~');
                                break;
                            // newline
                            case '%':
                                buf.Add('\n');
                                break;
                            // tab
                            case 'T':
                            case 't':
                                buf.Add('\t');
                                break;
                            // string
                            case 'S':
                            case 's':
                                buf.AddRange(((LispString)args[cmd_pos]).GetValue());
                                cmd_pos++;
                                break;
                            // integer
                            case 'D':
                            case 'd':
                                buf.AddRange(((LispNumber)args[cmd_pos]).GetValue().ToString());
                                cmd_pos++;
                                break;
                            // binary
                            case 'B':
                            case 'b':
                                buf.AddRange(Printer.AsBinary((LispNumber)args[cmd_pos]));
                                cmd_pos++;
                                break;
                            // hex
                            case 'X':
                            case 'x':
                                buf.AddRange(Printer.AsHex((LispNumber)args[cmd_pos]));
                                cmd_pos++;
                                break;
                            // float
                            case 'F':
                            case 'f':
                                buf.AddRange(((LispFloat)args[cmd_pos]).GetValue().ToString());
                                cmd_pos++;
                                break;
                            default:
                                throw new LispEvalException("format: Invalid format command ~" + fmt_cmd);
                        }
                        i++; // skip over cmd char
                    }
                }
                string buffer = new string(buf.ToArray());
                Console.WriteLine(buffer);
            }
            catch (InvalidCastException) {
                throw new LispEvalException("format: Invalid format string or argument");
            }
            // Console.WriteLine(Printer.Format(args, " ", true));
            return Null;
        });

        static public LispFunc println = new LispFunc(args =>
        {
            Console.WriteLine(Printer.Format(args, " ", false));
            return Null;
        });

        // static public LispFunc lisp_readline = new LispFunc(a =>
        // {
        //     var line = readline.Readline(((LispString)a[0]).GetValue());
        //     if (line == null)
        //         return Null;
        //     else
        //         return new LispString(line);
        // });

        static public LispFunc read_string = new LispFunc(args => Reader.ReadStr(((LispString)args[0]).GetValue()));
        static public LispFunc read_file = new LispFunc(args =>
        {
            try
            {
                return new LispString(File.ReadAllText(((LispString)args[0]).GetValue()));
            }
            catch (Exception e) when (e is ArgumentException)
            {
                throw new LispEvalException("Empty file argument");
            }
            catch (Exception e) when (e is FileNotFoundException)
            {
                throw new LispEvalException("File \"" + ((LispString)args[0]).GetValue() + "\" was not found.");
            }
        });

        // list/vector functions
        static public LispFunc list_Q = new LispFunc(args => args[0].GetType() == typeof(LispList) ? True : False);
        static public LispFunc vector_Q = new LispFunc(args => args[0].GetType() == typeof(LispVector) ? True : False);

        // hashmap functions
        static public LispFunc hash_map_Q = new LispFunc(args => args[0].GetType() == typeof(LispHashMap) ? True : False);
        static LispFunc contains_Q = new LispFunc(args =>
        {
            string key = ((LispString)args[1]).GetValue();
            var dict = ((LispHashMap)args[0]).GetValue();
            return dict.ContainsKey(key) ? True : False;
        });
        static LispFunc assoc = new LispFunc(args =>
        {
            var new_hm = ((LispHashMap)args[0]).Copy();
            return new_hm.assoc_BANG(args.Slice(1));
        });

        static LispFunc dissoc = new LispFunc(args =>
        {
            var new_hm = ((LispHashMap)args[0]).Copy();
            return new_hm.dissoc_BANG(args.Slice(1));
        });

        static LispFunc get = new LispFunc(args =>
        {
            string key = ((LispString)args[1]).GetValue();
            if (args[0] == Null)
            {
                return Null;
            }
            else
            {
                var dict = ((LispHashMap)args[0]).GetValue();
                return dict.ContainsKey(key) ? dict[key] : Null;
            }
        });

        static LispFunc keys = new LispFunc(args =>
        {
            var dict = ((LispHashMap)args[0]).GetValue();
            LispList key_lst = new LispList();
            foreach (var key in dict.Keys)
            {
                key_lst.conj_BANG(new LispString(key));
            }
            return key_lst;
        });

        static LispFunc vals = new LispFunc(args =>
        {
            var dict = ((LispHashMap)args[0]).GetValue();
            LispList val_lst = new LispList();
            foreach (var val in dict.Values)
            {
                val_lst.conj_BANG(val);
            }
            return val_lst;
        });

        static public LispFunc sequential = new LispFunc(args => args[0] is LispList ? True : False);

        static LispFunc cons = new LispFunc(args =>
        {
            var lst = new List<LispType>();
            lst.Add(args[0]);
            lst.AddRange(((LispList)args[1]).GetValue());
            return new LispList(lst);
        });

        static LispFunc concat = new LispFunc(a =>
        {
            if (a.Size() == 0) { return new LispList(); }
            var lst = new List<LispType>();
            lst.AddRange(((LispList)a[0]).GetValue());
            for (int i = 1; i < a.Size(); i++)
            {
                lst.AddRange(((LispList)a[i]).GetValue());
            }
            return new LispList(lst);
        });

        static LispFunc nth = new LispFunc(args =>
        {
            var idx = (int)((LispNumber)args[1]).GetValue();
            if (idx < ((LispList)args[0]).Size())
            {
                return ((LispList)args[0])[idx];
            }
            else
            {
                throw new LispException("nth: index out of range");
            }
        });

        static LispFunc first = new LispFunc(args => args[0] == Null ? Null : ((LispList)args[0])[0]);
        static LispFunc rest = new LispFunc(args => args[0] == Null ? new LispList() : ((LispList)args[0]).Rest());
        static LispFunc empty_Q = new LispFunc(args => ((LispList)args[0]).Size() == 0 ? True : False);
        static LispFunc length = new LispFunc(args =>
        {
            return (args[0] == Null) ? new LispNumber(0) : new LispNumber(((LispList)args[0]).Size());
        });

        static LispFunc conj = new LispFunc(args =>
        {
            var src_lst = ((LispList)args[0]).GetValue();
            var new_lst = new List<LispType>();
            new_lst.AddRange(src_lst);
            if (args[0] is LispVector)
            {
                for (int i = 1; i < args.Size(); i++)
                {
                    new_lst.Add(args[i]);
                }
                return new LispVector(new_lst);
            }
            else
            {
                for (int i = 1; i < args.Size(); i++)
                {
                    new_lst.Insert(0, args[i]);
                }
                return new LispList(new_lst);
            }
        });

        static LispFunc seq = new LispFunc(args =>
        {
            if (args[0] == Null)
            {
                return Null;
            }
            else if (args[0] is LispVector)
            {
                return (((LispVector)args[0]).Size() == 0) ? Null : new LispList(((LispVector)args[0]).GetValue());
            }
            else if (args[0] is LispList)
            {
                return (((LispList)args[0]).Size() == 0) ? Null : args[0];
            }
            else if (args[0] is LispString)
            {
                var s = ((LispString)args[0]).GetValue();
                if (s.Length == 0)
                {
                    return Null;
                }
                var chars_list = new List<LispType>();
                foreach (var c in s)
                {
                    chars_list.Add(new LispString(c.ToString()));
                }
                return new LispList(chars_list);
            }
            return Null;
        });

        // general list related functions
        static LispFunc apply = new LispFunc(args =>
        {
            var f = (LispFunc)args[0];
            var lst = new List<LispType>();
            lst.AddRange(args.Slice(1, args.Size() - 1).GetValue());
            lst.AddRange(((LispList)args[args.Size() - 1]).GetValue());
            return f.Apply(new LispList(lst));
        });

        static LispFunc map = new LispFunc(args =>
        {
            LispFunc f = (LispFunc)args[0];
            var src_lst = ((LispList)args[1]).GetValue();
            var new_lst = new List<LispType>();
            for (int i = 0; i < src_lst.Count; i++)
            {
                new_lst.Add(f.Apply(new LispList(src_lst[i])));
            }
            return new LispList(new_lst);
        });


        // metadata functions
        static LispFunc meta = new LispFunc(args => args[0].GetMeta());
        static LispFunc with_meta = new LispFunc(args => (args[0]).Copy().SetMeta(args[1]));

        // atom functions
        static LispFunc atom_Q = new LispFunc(args => args[0] is LispAtom ? True : False);
        static LispFunc deref = new LispFunc(args => ((LispAtom)args[0]).GetValue());
        static LispFunc reset = new LispFunc(args => ((LispAtom)args[0]).SetValue(args[1]));
        static LispFunc swap = new LispFunc(args =>
        {
            LispAtom atom = (LispAtom)args[0];
            LispFunc f = (LispFunc)args[1];
            var new_lst = new List<LispType>();
            new_lst.Add(atom.GetValue());
            new_lst.AddRange((args.Slice(2)).GetValue());
            return atom.SetValue(f.Apply(new LispList(new_lst)));
        });

        // misc
        static LispFunc exit = new LispFunc(args => { if (args.Size() == 0) { Environment.Exit(0); } return Null; });
        static LispFunc clear = new LispFunc(args => { Console.Clear(); return Null; });
        static LispFunc split = new LispFunc(args =>
        {
            LispList list = new LispList();
            LispString val = (LispString)args[0];
            LispString delim = (LispString)args[1];
            string[] vals = val.GetValue().Split(delim.GetValue().ToCharArray()[0]);
            foreach (string str in vals) {
                list.Add(new LispString(str));
            }
            return list;
        });

        // TODO 
        //static LispFunc the_as = new LispFunc(args =>
        //{
        //    LispSymbol cast = (LispSymbol)args[0];
        //    switch (cast.GetName())
        //    {
        //        case "int":
        //            LispType val = args[1];
        //            try {
        //                
        //            }
        //            break;
        //    }
        //});

        // static LispFunc readline = new LispFunc(args =>
        // {
        //     LispString filename = (LispString)args[0];
        //     var lines = File.ReadLines(filename.GetValue());
        //     LispList list = new LispList();
        //     foreach (string line in lines) {
        //         list.Add(new LispString(line));
        //     }
        //     return list;
        // });

        // static LispFunc slice_at = new LispFunc(args =>
        // {
        //     LispList list = (LispList)args[0];
        //     LispType val = args[1];
        //     for (int i = 0; i < list.Size(); i++)
        //     {
        //         if (Types.Equals(list.nth(i), val))
        //         {
        //             LispList slicedList = list.Slice(0, i);
        //         }
        //     }
        // });

        /// <summary>
        /// dictionary to map symbols to functions
        /// </summary>
        static public Dictionary<string, LispType> core = new Dictionary<string, LispType>
        {
            {"=",  new LispFunc(args => Types.Equals(args[0], args[1]) ? True : False)},
            {"throw", _throw},
            {"null?", null_Q},
            {"true?", true_Q},
            {"false?", false_Q},
            {"symbol", new LispFunc(args => new LispSymbol((LispString)args[0]))},
            {"symbol?", symbol_Q},
            {"string?", string_Q},
            {"keyword", keyword},
            {"keyword?", keyword_Q},
            {"number?", number_Q},
            {"fn?", function_Q},
            {"macro?", macro_Q},

            {"format-str", format_str},
            {"str", str},
            {"format", format},
            {"println", println},
            // {"readline", lisp_readline},
            {"read-string", read_string},
            {"read-file", read_file},
            {"<",  new LispFunc(args => (LispNumber)args[0] <  (LispNumber)args[1])},
            {"<=", new LispFunc(args => (LispNumber)args[0] <= (LispNumber)args[1])},
            {">",  new LispFunc(args => (LispNumber)args[0] >  (LispNumber)args[1])},
            {">=", new LispFunc(args => (LispNumber)args[0] >= (LispNumber)args[1])},
            {"+",  new LispFunc(args => (LispNumber)args[0] +  (LispNumber)args[1])},
            {"-",  new LispFunc(args => (LispNumber)args[0] -  (LispNumber)args[1])},
            {"*",  new LispFunc(args => (LispNumber)args[0] *  (LispNumber)args[1])},
            {"/",  new LispFunc(args => (LispNumber)args[0] /  (LispNumber)args[1])},
            {"time-ms", time_ms},

            {"list",  new LispFunc(a => new LispList(a.GetValue()))},
            {"list?", list_Q},
            {"vector",  new LispFunc(a => new LispVector(a.GetValue()))},
            {"vector?", vector_Q},
            {"hash-map",  new LispFunc(a => new LispHashMap(a))},
            {"map?", hash_map_Q},
            {"contains?", contains_Q},
            {"assoc", assoc},
            {"dissoc", dissoc},
            {"get", get},
            {"keys", keys},
            {"vals", vals},

            {"sequential?", sequential},
            {"cons", cons},
            {"concat", concat},
            {"vec",  new LispFunc(args => new LispVector(((LispList)args[0]).GetValue()))},
            {"nth", nth},
            {"first", first},
            {"rest",  rest},
            {"empty?", empty_Q},
            {"length", length},
            {"conj", conj},
            {"seq", seq},
            {"apply", apply},
            {"map", map},

            {"with-meta", with_meta},
            {"meta", meta},
            {"atom", new LispFunc(args => new LispAtom(args[0]))},
            {"atom?", atom_Q},
            {"deref", deref},
            {"reset!", reset},
            {"swap!", swap},

            {"split", split},
            {"e", exit},
            {"exit", exit},
            {"clear", clear}
        };
    }
}
