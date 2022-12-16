using static LispInterpreter.Types;

namespace LispInterpreter
{
    /// <summary>
    /// main entry point that starts the REPL and sets up the environment
    /// </summary>
    internal class Entrypoint
    {
        static void Main(string[] args)
        {
            var textReader = Console.In;
            Console.Title = "Lisp Interpreter";
            REPL repl = new REPL();
            Env env = new Env(null);
            Func<string, LispType> ReadEval = (string str) => repl.Eval(repl.Read(str), env);

            env.Set(new LispSymbol("eval"), new LispFunc(args => repl.Eval(args[0], env)));
            ReadEval("(define eval-file (lambda (f) (eval (read-string (str \"(begin \" (read-file f) \"\nnull)\")))))");

            // add built-ins
            foreach (var entry in Core.core)
            {
                env.Set(new LispSymbol(entry.Key), entry.Value);
            }

            // read core lib
            ReadEval("(eval-file \"core-lib.lisp\")");

            while (repl.alive)
            {
                string line;
                try
                {
                    Console.Write("lisp> ");
                    line = Console.ReadLine();
                    if (line == null)
                        break;
                    if (line == string.Empty)
                        continue;
                }
                catch (IOException e)
                {
                    Console.WriteLine("IOException: " + e.Message);
                    break;
                }
                try
                {
                    string? eval = repl.Print(ReadEval(line));
                    // don't write null out to console
                    if (eval != null)
                        Console.WriteLine(eval);
                }
                catch (LispContinue)
                {
                    continue;
                }
                catch (LispException e)
                {
                    Console.WriteLine(e.Message + "\nForm:\n" + line);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    continue;
                }
            }
        }
    }
}
