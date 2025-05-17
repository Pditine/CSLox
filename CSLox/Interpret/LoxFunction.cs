using CSLox.Parse;

namespace CSLox.Interpret;

class LoxFunction(Function declaration, Environment closure, bool isInitializer) : ILoxCallAble
{
    public int Arity => declaration.parameters.Count;
    public object Call(Interpreter interpreter, List<object> arguments)
    {
        Environment environment = new Environment(closure);
        for (int i = 0; i < declaration.parameters.Count; i++)
        {
            environment.Define(declaration.parameters[i].Lexeme, arguments[i]);
        }
        try 
        { 
            interpreter.ExecuteBlock(declaration.body, environment); 
        } 
        catch (Return returnValue)
        {
            if (isInitializer) return closure.GetAt(0, "this");
            return returnValue.Value;
        }
        if (isInitializer) return closure.GetAt(0, "this");
        return null;
    }
    
    public override string ToString()
    {
        return "<fn " + declaration.name.Lexeme + ">";
    }
    
    public LoxFunction Bind(LoxInstance instance)
    {
        Environment environment = new Environment(closure);
        environment.Define("this", instance);
        return new LoxFunction(declaration, environment, isInitializer);
    }
}

class Return(object value) : RuntimeError(null, null)
{
    public object Value => value;
}