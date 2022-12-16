using static LispInterpreter.Types;

namespace LispInterpreter
{
    /// <summary>
    /// read-eval-print-loop, the heart of the operation
    /// </summary>
    public class REPL
    {
        // public string? lastExpression;
        public bool alive = true;
        public Util util = new Util();

        public LispType Read(string exp)
        {
            return Reader.ReadStr(exp);
        }

        /// <summary>
        /// evaluate expression and handle special forms: let, define, if, lambda, etc.
        /// </summary>
        /// <param name="ast">Abstract symbol tree</param>
        /// <param name="env">Environment</param>
        /// <returns>A Lisp type</returns>
        public LispType Eval(LispType ast, Env env)
        {
            LispType a0, a1, a2, result;
            LispList el;

            while (true)
            {
                if (!ast.IsList())
                {
                    return EvalAST(ast, env);
                }

                LispType exp = MacroExpand(ast, env);
                if (!exp.IsList()) {
                    return EvalAST(exp, env);
                }
                
                LispList astList = (LispList)exp;

                if (astList.Size() == 0)
                {
                    return astList;
                }
                a0 = astList[0];
                // if (!(a0 is LispSymbol))
                // {
                //     throw new LispException("attempt to apply on non-symbol '" + Printer.PrintStr(a0, true) + "'");
                // }

                string a0sym = a0 is LispSymbol ? ((LispSymbol)a0).GetName()
                                   : "__<*lambda*>__";


                // handle special forms
                switch (a0sym)
                {
                    // bind a symbol to a value
                    case "define":
                        a1 = astList[1];
                        a2 = astList[2];
                        result = Eval(a2, env);
                        env.Set((LispSymbol)a1, result);
                        return result;
                    // define symbols that are valid within the let
                    case "let":
                        a1 = astList[1];
                        a2 = astList[2];
                        LispSymbol key;
                        LispType val;
                        Env elt = new Env(env);
                        for (int i = 0; i < ((LispList)a1).Size(); i += 2)
                        {
                            key = (LispSymbol)((LispList)a1)[i];
                            val = ((LispList)a1)[i + 1];
                            elt.Set(key, Eval(val, elt));
                        }
                        ast = a2;
                        env = elt;
                        break;
                    // begin form, execute all forms in body in order, return value is the last form in the list
                    case "begin":
                        el = (LispList)EvalAST(astList.Slice(1, astList.Size() - 1), env);
                        ast = astList[astList.Size() - 1];
                        break;
                    // if statement, has an optional false form that gets evaluated if the cond is false/null
                    case "if":
                        a1 = astList[1];
                        LispType cond = Eval(a1, env);
                        if (cond == Null || cond == False)
                        {
                            if (astList.Size() > 3)
                            {
                                ast = astList[3];
                            }
                            else
                            {
                                throw new LispEvalException("'if' form did not contain any arguments.");
                            }
                        }
                        else
                        {
                            a2 = astList[2];
                            ast = a2;
                        }
                        break;
                    // lambda form, create anonymous function
                    case "lambda":
                        LispList a1fn = (LispList)astList[1];
                        LispType a2fn = astList[2];
                        Env fnEnv = env;
                        return new LispFunc(args => Eval(a2fn, new Env(fnEnv, a1fn, args)));
                    // quote form, return given symbol as is
                    case "quote":
                        return astList[1];
                    case "quasiquote-expand":
                        return Quasiquote(astList[1]);
                    // quasiquote form, return given value as is, but evaluate anything that is unquoted
                    case "quasiquote":
                        ast = Quasiquote(astList[1]);
                        break;
                    // macro declaration
                    case "defmacro":
                        a1 = astList[1];
                        a2 = astList[2];
                        result = Eval(a2, env);
                        result.Copy();
                        ((LispFunc)result).SetMacro(true);
                        env.Set((LispSymbol)a1, result);
                        return result;
                    // expand macros without evaluating
                    case "macro-expand":
                        a1 = astList[1];
                        if (!Macro(a1, env)) {
                            throw new LispEvalException("macro-expand: \"" + Printer.PrintStr(a1, true) + "\" is not a macro");
                        }
                        return MacroExpand(a1, env);
                    // dotimes form, basically for loop
                    case "dotimes":
                        a1 = (LispList)astList[1];
                        a2 = astList[2];
                        LispType dotimes_res = Null;
                        LispList dotimes_a1_lst = (LispList)a1;
                        LispSymbol? dotimes_sym = null;
                        LispNumber dotimes_iter;
                        if (a1 is LispList) {
                            try {
                                dotimes_sym = (LispSymbol)dotimes_a1_lst[0];
                                dotimes_iter = (LispNumber)dotimes_a1_lst[1];
                                int dotimes_iterval = (int)dotimes_iter.GetValue();
                                env.Set(dotimes_sym, new LispNumber(0));
                                for (int i = 0; i < dotimes_iterval; i++) {
                                    env.data[dotimes_sym.GetName()] = new LispNumber(i);
                                    dotimes_res = Eval(a2, env);
                                }
                                // delete iterator symbol from env after evaluation
                                if (dotimes_sym != null) {
                                    env.data.Remove(dotimes_sym.GetName());
                                }
                                return dotimes_res;
                            }
                            catch (InvalidCastException) {
                                throw new LispEvalException("dotimes: invalid symbol or iterator used");
                            }
                        }
                        return Null;
                    // countdown, decrementing for loop
                    case "countdown":
                        a1 = (LispList)astList[1];
                        a2 = astList[2];
                        LispType countdown_res = Null;
                        LispList countdown_a1_lst = (LispList)a1;
                        LispSymbol? countdown_sym = null;
                        LispNumber countdown_iter;
                        if (a1 is LispList) {
                            try {
                                countdown_sym = (LispSymbol)countdown_a1_lst[0];
                                countdown_iter = (LispNumber)countdown_a1_lst[1];
                                int countdown_iterval = (int)countdown_iter.GetValue();
                                env.Set(countdown_sym, new LispNumber(countdown_iterval));
                                for (int i = countdown_iterval; i < 0; i--) {
                                    env.data[countdown_sym.GetName()] = new LispNumber(i);
                                    countdown_res = Eval(a2, env);
                                }
                                // delete iterator symbol from env after evaluation
                                if (countdown_sym != null) {
                                    env.data.Remove(countdown_sym.GetName());
                                }
                                return countdown_res;
                            }
                            catch (InvalidCastException) {
                                throw new LispEvalException("countdown: invalid symbol or iterator used");
                            }
                        }
                        return Null;
                    case "and":
                        a1 = astList[1];
                        a2 = astList[2];
                        for (int i = 0; i < astList.Size(); i++) {
                            LispType and_res = Eval(astList[1 + i], env);
                            if (and_res == False) {
                                return False;
                            }
                        }
                        return True;
                    case "set!":
                        a1 = astList[1];
                        a2 = astList[2];
                        LispSymbol set_sym = (LispSymbol)a1;
                        LispType set_result = Eval(a2, env);
                        if (env.Find(set_sym) != null) {
                            env.data[((LispSymbol)a1).GetName()] = Eval(set_result, env);
                        }
                        else {
                            throw new LispEvalException("set!: The symbol " + set_sym.GetName() + " was not found in the dictionary.");
                        }
                        return set_result;
                    // documentation
                    case "man":
                    case "help":
                        a1 = astList[1];
                        if (a1 == Null)
                            Util.PrintDefaultHelp();
                        else
                            util.man((LispSymbol)a1, env);
                        return Null;
                    // try/catch
                    case "try":
                        try
                        {
                            return Eval(astList[1], env);
                        }
                        catch (Exception e)
                        {
                            if (astList.Size() > 2)
                            {
                                LispType ex;
                                a2 = astList[2];
                                LispType a2_ = ((LispList)a2)[0];
                                if (((LispSymbol)a2_).GetName() == "catch")
                                {
                                    if (e is LispException)
                                    {
                                        ex = ((LispException)e).GetValue();
                                    }
                                    else
                                    {
                                        ex = new LispString(e.Message);
                                    }
                                    return Eval(((LispList)a2)[2], new Env(env, ((LispList)a2).Slice(1, 2), new LispList(ex)));
                                }
                            }
                            throw e;
                        }
                    // not matching any special form, just eval
                    default:
                        el = (LispList)EvalAST(ast, env);
                        try
                        {
                            var f = (LispFunc)el[0];
                            LispType? fn_ast = f.GetAst();
                            if (fn_ast != null)
                            {
                                ast = fn_ast;
                                env = f.GenEnv(el.Rest());
                            }
                            return f.Apply(el.Rest());
                        }
                        catch (InvalidCastException)
                        {
                            throw new LispEvalException("Typecheck failed. For function call head, got " + "\"" + Printer.PrintStr(el[0], true) + "\"" + " when expecting a function.");
                        }
                }
            }
        }

        /// <summary>
        /// env lookup and expression evaluation
        /// </summary>
        /// <param name="ast">Abstract symbol tree</param>
        /// <param name="env">Environment</param>
        /// <returns>A Lisp type</returns>
        /// <exception cref="LispException"></exception>
        public LispType EvalAST(LispType ast, Env env)
        {
            if (ast is LispSymbol)
            {
                LispSymbol sym = (LispSymbol)ast;
                try
                {
                    var result = env.Get(sym);
                    return result;
                }
                catch (KeyNotFoundException)
                {
                    throw new LispEvalException("The symbol \"" + sym.GetName() + "\" was looked up in the dictionary, but it does not exist.");
                }
            }
            else if (ast is LispList)
            {
                LispList listOld = (LispList)ast;
                LispList listNew = listOld.IsList() ? new LispList() : new LispVector();
                foreach (LispType val in listOld.GetValue())
                {
                    listNew.Add(Eval(val, env));
                }
                return listNew;
            }
            else if (ast is LispHashMap)
            {
                var newDict = new Dictionary<string, LispType>();
                foreach (var entry in ((LispHashMap)ast).GetValue())
                {
                    newDict.Add(entry.Key, Eval(entry.Value, env));
                }
                return new LispHashMap(newDict);
            }
            else
            {
                return ast;
            }
        }

        // does the ast start with the specified symbol?
        public static bool StartsWith(LispType ast, string sym)
        {
            if (ast is LispList && !(ast is LispVector))
            {
                LispList lst = (LispList)ast;
                if (lst.Size() == 2 && lst[0] is LispSymbol)
                {
                    LispSymbol a0 = (LispSymbol)lst[0];
                    return a0.GetName() == sym;
                }
            }
            return false;
        }

        public static LispType QuasiquoteLoop(LispList ast)
        {
            LispType acc = new LispList();
            for (int i = ast.Size() - 1; 0 <= i; i -= 1)
            {
                LispType elt = ast[i];
                if (StartsWith(elt, "unquote-splice"))
                {
                    acc = new LispList(new LispSymbol("concat"), ((LispList)elt)[1], acc);
                }
                else
                {
                    acc = new LispList(new LispSymbol("cons"), Quasiquote(elt), acc);
                }
            }
            return acc;
        }

        // handle quasiquote form
        public static LispType Quasiquote(LispType ast)
        {
            if (ast is LispVector)
            {
                return new LispList(new LispSymbol("vec"), QuasiquoteLoop((LispList)ast));
            }
            else if (StartsWith(ast, "unquote"))
            {
                return ((LispList)ast)[1];
            }
            else if (ast is LispList)
            {
                return QuasiquoteLoop((LispList)ast);
            }
            else if (ast is LispSymbol || ast is LispHashMap)
            {
                return new LispList(new LispSymbol("quote"), ast);
            }
            else
            {
                return ast;
            }
        }

        // is the given form a macro?
        public static bool Macro(LispType ast, Env env) {
            if (ast is LispList) {
                LispType a0 =((LispList)ast)[0];
                if (a0 is LispSymbol && env.Find((LispSymbol)a0) != null) {
                    LispType macro = env.Get((LispSymbol)a0);
                    if (macro is LispFunc && ((LispFunc)macro).IsMacro()) {
                        return true;
                    }
                }
            }
            return false;
        }

        // expand a given macro
        public static LispType MacroExpand(LispType ast, Env env) {
            while (Macro(ast, env)) {
                LispSymbol a0 = (LispSymbol)((LispList)ast)[0];
                LispFunc macro = (LispFunc)env.Get(a0);
                ast = macro.Apply(((LispList)ast).Rest());
            }
            return ast;
        }

        public string? Print(LispType exp)
        {
            return exp == Null ? null : Printer.PrintStr(exp);
        }
    }
}