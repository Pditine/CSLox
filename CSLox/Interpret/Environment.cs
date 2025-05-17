using CSLox.Scan;

namespace CSLox.Interpret;

class Environment
{ 
    private readonly Dictionary<string, object> _values = new();
    private Environment _enclosing;
    public Environment Enclosing => _enclosing;

    public Environment()
    {
        _enclosing = null;
    }
    
    public Environment(Environment enclosing)
    {
        _enclosing = enclosing;
    }
    
    public void Define(string name, object value) 
    {
        // For some reason, we allow users to redefine variables in global environment
        // like this:
        // var a = "before";
        // var a = "after";
        
        // So we don't need to check if the variable already exists.
        // _values.Add(name, value);
        _values[name] = value;
    }

    public object Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out var value))
        {
            return value;
        }
        if (_enclosing != null)
        {
            return _enclosing.Get(name);
        }
        
        throw new RuntimeError(name, "Undefined variable '" + name.Lexeme + "'.");
    }

    public void Assign(Token name, Object value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }
        if (_enclosing != null)
        {
            _enclosing.Assign(name, value);
            return;
        }
        throw new RuntimeError(name, "Undefined variable '" + name.Lexeme + "'.");
    }
    
    public void AssignAt(int distance, Token name, object value)
    {
        Ancestor(distance)._values[name.Lexeme] = value;
    }
    
    public object GetAt(int distance, string name)
    {
        return Ancestor(distance)._values[name];
    }
    
    private Environment Ancestor(int distance)
    {
        Environment environment = this;
        for (int i = 0; i < distance; i++)
        {
            environment = environment._enclosing;
        }
        return environment;
    }
}