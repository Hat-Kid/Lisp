using System.Text.Json;
using System.Text.Json.Serialization;
using static LispInterpreter.Types;

// utility class, used for doc generation
namespace LispInterpreter
{
    public class ManPage
    {
        [JsonInclude]
        public string? name;
        [JsonInclude]
        public List<string>? args;
        [JsonInclude]
        public string? desc;
        [JsonInclude]
        public List<string>? returns;
    }

    public class ManPages
    {
        public List<ManPage> pages { get; set; }
    }

    public class Util
    {
        ManPages? docs;

        public Util()
        {
            if (File.Exists("docs.json"))
                docs = JsonSerializer.Deserialize<ManPages>(File.ReadAllText("docs.json"));
        }

        public void man(LispSymbol func, Env env)
        {
            if (env.Find(func) != null)
            {
                if (docs != null)
                {
                    ManPage? page = docs.pages.Find(man => man.name == func.GetName());
                    if (page != null)
                    {
                        Console.WriteLine("Signature: (defun " + page.name + " (function " + string.Join(' ', page.args) + " " + page.returns[0] + "))\n");
                        Console.WriteLine(page.desc + "\n");
                        Console.WriteLine("Returns: " + page.returns[1]);
                    }
                    else
                    {
                        Console.WriteLine("No man page for " + func.GetName());
                    }
                }
            }
            else
            {
                Console.WriteLine("Function \"" + func.GetName() + "\" does not exist.");
            }
        }

        public static void PrintDefaultHelp()
        {
            Console.WriteLine($@"
This is a Lisp interpreter. It can evaluate basic
Lisp expressions, such as (+ 1 2) or (define a 25).

List of a few built-ins:
(+ <arg0> <arg1>) - Add two numbers.
(list <values...>) - Create a list out of the given values. (list) creates an empty list.
(define <sym-name> <value>) - Bind a symbol to a value.
(defmacro <sym-name> <value>) - Define a macro.
(let (<name> <value>) ...) - Define multiple symbols that are valid within the let.
(if cond true-case [false-case]) - If statement. If the condition is true, evaluates the true-case,
if the optional false-case exists, evaluate that, otherwise, return #f.
(lambda (<argument-list>) ...) - Define an anonymous function.

For detailed information on a single function (if available), try out (man <function-name>).

Types:
int - Any integer value, e.g. 10. Hexadecimal values can also be used, e.g. #x231 or 0x3343943. Backed by C#'s BigInteger, so they can be as large as you want.
float - Floating point number, e.g. 2.25.
string - Sequence of characters enclosed in quotes.
list - A list of values enclosed in parens, e.g (1 2 3).
symbol - A literal, e.g 'sym'. Symbols can be bound to values using 'define'. For example, (define var (+ 10 15)) will bind the result of (+ 10 15) to 'var'.

Constants:
#t - true
#f - false
null - null value
");
        }
    }
}
