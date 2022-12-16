using static LispInterpreter.Types;

namespace LispInterpreter
{
    /// <summary>
    /// the interpreter environment, has a dictionary that holds all symbol definitions
    /// </summary>
    public class Env
    {
        Env? outer = null;
        public Dictionary<string, LispType> data = new Dictionary<string, LispType>();
        public int gensym_id = 0;

        public Env(Env? outer)
        {
            this.outer = outer;
        }

        public Env(Env outer, LispList binds, LispList exps)
        {
            this.outer = outer;
            for (int i = 0; i < binds.Size(); i++)
            {
                string sym = ((LispSymbol)binds.nth(i)).GetName();
                if (sym == "&rest")
                {
                    data[((LispSymbol)binds.nth(i + 1)).GetName()] = exps.Slice(i);
                    break;
                }
                else
                {
                    data[sym] = exps.nth(i);
                }
            }
        }

        public void Set(LispSymbol key, LispType val)
        {
            try {
                data.Add(key.GetName(), val);
            } catch (ArgumentException) {
                throw new LispEvalException("Symbol '" + key.GetName() + "' already exists in the environment.");
            }
        }

        public Env? Find(LispSymbol key)
        {
            if (data.ContainsKey(key.GetName()))
            {
                return this;
            }
            if (outer != null)
            {
                return outer.Find(key);
            }
            else
            {
                return null;
            }
        }

        // Get value from symbol
        public LispType Get(LispSymbol key)
        {
            Env? env = Find(key);
            if (env != null)
            {
                return env.data[key.GetName()];
            }
            else
            {
                // throw new LispEvalException("The symbol '" + key.GetName() + "' was looked up in the dictionary, but it does not exist.");
                throw new KeyNotFoundException();
            }
        }

        // TODO gensym
        // public LispType Gensym(LispType val) {
        //     LispSymbol sym = (LispSymbol)val;
        //     Set(new LispSymbol(sym.GetName() + "-" + gensym_id++), val);
        // }
    }
}
