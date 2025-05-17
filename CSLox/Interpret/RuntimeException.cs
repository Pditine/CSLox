using CSLox.Scan;

namespace CSLox.Interpret;

class RuntimeError(Token token, String message) : Exception(message)
{ 
    public readonly Token Token = token;
}