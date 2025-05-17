using CSLox.Scan;

namespace CSLox.Interpret;

class LoxClass : ILoxCallAble
{
    private readonly string _name;
    private readonly LoxClass _superclass;
    private readonly Dictionary<string, LoxFunction> _methods;
    public LoxClass(string name, LoxClass superclass, Dictionary<string, LoxFunction> methods)
    {
        _name = name;
        _superclass = superclass;
        _methods = methods;
    }

    public override string ToString()
    {
        return _name;
    }

    public int Arity
    {
        get
        {
            LoxFunction initializer = FindMethod("init");
            if (initializer == null) return 0;
            return initializer.Arity;
        }
    }
    public object Call(Interpreter interpreter, List<object> arguments)
    {
        LoxInstance instance = new LoxInstance(this);
        LoxFunction initializer = FindMethod("init"); 
        if (initializer != null)
        {
            initializer.Bind(instance).Call(interpreter, arguments);
        }
        return instance;
    }
    
    public LoxFunction FindMethod(string name)
    {
        if (_methods.TryGetValue(name, out var method))
        {
            return method;
        }
        
        if (_superclass != null)
        {
            return _superclass.FindMethod(name);
        }
        return null;
    }
}

class LoxInstance
{
    private LoxClass _klass;
    private readonly Dictionary<string, object> _fields = new();

    public LoxInstance(LoxClass klass)
    {
        _klass = klass;
    }
    
    public object Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out var field))
        {
            return field;
        }
        
        LoxFunction method = _klass.FindMethod(name.Lexeme); 
        if (method != null) return method.Bind(this);

        throw new RuntimeError(name, "Undefined property '" + name.Lexeme + "'.");
    }
    
    public void Set(Token name, object value)
    {
        _fields[name.Lexeme] = value;
    }
    
    public override string ToString()
    {
        return _klass + " instance";
    }
}