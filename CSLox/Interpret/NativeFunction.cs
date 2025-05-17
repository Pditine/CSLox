namespace CSLox.Interpret;

class LoxClock : ILoxCallAble
{
    public int Arity => 0;
    public object Call(Interpreter interpreter, List<object> arguments)
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }
    
    public override string ToString()
    {
        return "<native fn>";
    }
}