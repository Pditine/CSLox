namespace CSLox.Interpret;

interface ILoxCallAble
{
    int Arity { get; }
    object Call(Interpreter interpreter, List<object> arguments);
}