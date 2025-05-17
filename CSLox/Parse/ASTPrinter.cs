using System.Text;

namespace CSLox.Parse;

/// <summary>
/// I'm too lazy to maintain the class.
/// </summary>
class ASTPrinter : Expr.IVisitor<string>
{
    public string Print(Expr expr) 
    { 
        return expr.Accept(this); 
    }

    public string VisitAssign(Assign expr)
    {
        throw new NotImplementedException();
    }

    public string VisitBinary(Binary expr)
    {
        return Parenthesize(expr.operatorToken.Lexeme, expr.left, expr.right); 
    }

    public string VisitCall(Call expr)
    {
        throw new NotImplementedException();
    }

    public string VisitGet(Get expr)
    {
        throw new NotImplementedException();
    }

    public string VisitGrouping(Grouping expr)
    {
        return Parenthesize("group", expr.expression);
    }

    public string VisitLiteral(Literal expr)
    {
        if (expr.value == null) return "nil"; 
        return expr.value.ToString();
    }

    public string VisitLogical(Logical expr)
    {
        throw new NotImplementedException();
    }

    public string VisitSet(Set expr)
    {
        throw new NotImplementedException();
    }

    public string VisitSuper(Super expr)
    {
        throw new NotImplementedException();
    }

    public string VisitThis(This expr)
    {
        throw new NotImplementedException();
    }

    public string VisitUnary(Unary expr)
    {
        return Parenthesize(expr.operatorToken.Lexeme, expr.right);
    }

    public string VisitVariable(Variable expr)
    {
        throw new NotImplementedException();
    }

    private string Parenthesize(string name, params Expr[] exprs)
    { 
        StringBuilder builder = new StringBuilder(); 
        builder.Append('(').Append(name); 
        foreach (Expr expr in exprs)
        { 
            builder.Append(' '); 
            builder.Append(expr.Accept(this)); 
        }                       
        builder.Append(')'); 
        return builder.ToString();
    }
}