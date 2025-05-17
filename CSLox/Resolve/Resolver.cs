using CSLox.Interpret;
using CSLox.Parse;
using CSLox.Scan;
using Return = CSLox.Parse.Return;

namespace CSLox.Resolve;

class Resolver : Expr.IVisitor<VOID?>, Stmt.IVisitor<VOID?>
{
    private Interpreter _interpreter;
    private readonly List<Dictionary<string, bool>> _scopes = new();
    private FunctionType _currentFunction = FunctionType.NONE;
    private ClassType _currentClass = ClassType.NONE;
    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
    }

    public VOID? VisitAssign(Assign expr)
    {
        Resolve(expr.value);
        ResolveLocal(expr, expr.name);
        return null;
    }

    public VOID? VisitBinary(Binary expr)
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return null;
    }

    public VOID? VisitCall(Call expr)
    {
        Resolve(expr.callee);
        foreach (Expr argument in expr.arguments)
        {
            Resolve(argument);
        }
        return null;
    }

    public VOID? VisitGet(Get expr)
    {
        Resolve(expr.exprObject);
        return null;
    }

    public VOID? VisitGrouping(Grouping expr)
    {
        Resolve(expr.expression);
        return null;
    }

    public VOID? VisitLiteral(Literal expr)
    {
        return null;
    }

    public VOID? VisitLogical(Logical expr)
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return null;
    }

    public VOID? VisitSet(Set expr)
    {
        Resolve(expr.exprObject);
        Resolve(expr.value);
        return null;
    }

    public VOID? VisitSuper(Super expr)
    {
        if (_currentClass == ClassType.NONE)
        {
            Lox.Error(expr.keyword, "Can't use 'super' outside of a class.");
        }
        else if (_currentClass != ClassType.SUBCLASS)
        {
            Lox.Error(expr.keyword, "Can't use 'super' in a class with no superclass.");
        }
        ResolveLocal(expr, expr.keyword);
        return null;
    }

    public VOID? VisitThis(This expr)
    {
        if (_currentClass == ClassType.NONE)
        {
            Lox.Error(expr.keyword, "Can't use 'this' outside of a class.");
            return null;
        }
        ResolveLocal(expr, expr.keyword);
        return null;
    }

    public VOID? VisitUnary(Unary expr)
    {
        Resolve(expr.right);
        return null;
    }

    public VOID? VisitVariable(Variable expr)
    {
        if (_scopes.Count != 0 && _scopes.Last().TryGetValue(expr.name.Lexeme, out var isDefined) && isDefined == false)
        {
            Lox.Error(expr.name, "Can't read local variable in its own initializer.");
        } 
        ResolveLocal(expr, expr.name);
        return null;
    }

    public VOID? VisitBlock(Block stmt)
    {
        BeginScope();
        Resolve(stmt.statements);
        EndScope();
        return null;
    }

    public VOID? VisitClass(Class stmt)
    {
        var enclosingClass = _currentClass; 
        _currentClass = ClassType.CLASS;
        
        Declare(stmt.name);
        Define(stmt.name);
        
        if (stmt.superclass != null && stmt.name.Lexeme.Equals(stmt.superclass.name.Lexeme))
        {
            Lox.Error(stmt.superclass.name, "A class can't inherit from itself.");
        }
        
        if (stmt.superclass != null) 
        {
            _currentClass = ClassType.SUBCLASS;
            Resolve(stmt.superclass);
        }
        
        if (stmt.superclass != null) 
        {
            BeginScope();
            _scopes.Last()["super"] = true;
        }
        
        BeginScope(); 
        _scopes.Last()["this"] = true;
        foreach (Function method in stmt.methods)
        {
            FunctionType declaration = FunctionType.METHOD;
            if (method.name.Lexeme.Equals("init"))
            {
                declaration = FunctionType.INITIALIZER;
            }

            ResolveFunction(method, declaration);
        }
        EndScope();
        if (stmt.superclass != null) EndScope();
        
        _currentClass = enclosingClass;
        return null;
    }

    public VOID? VisitExpression(Expression stmt)
    {
        Resolve(stmt.expression);
        return null;
    }

    public VOID? VisitFunction(Function stmt)
    {
        Declare(stmt.name); 
        Define(stmt.name); 
        ResolveFunction(stmt, FunctionType.FUNCTION); 
        return null; 
    }

    public VOID? VisitIf(If stmt)
    {
        Resolve(stmt.condition);
        Resolve(stmt.thenBranch);
        if (stmt.elseBranch != null)
        {
            Resolve(stmt.elseBranch);
        }
        return null;
    }

    public VOID? VisitPrint(Print stmt)
    {
        Resolve(stmt.expression);
        return null;
    }

    public VOID? VisitReturn(Return stmt)
    {
        if (_currentFunction == FunctionType.NONE)
        {
            Lox.Error(stmt.keyword, "Can't return from top-level code.");
        }
        if (stmt.value != null)
        {
            if (_currentFunction == FunctionType.INITIALIZER)
            {
                Lox.Error(stmt.keyword, "Can't return a value from an initializer.");
            }
            Resolve(stmt.value);
        }
        return null;
    }

    public VOID? VisitVar(Var stmt)
    {
        Declare(stmt.name);
        if (stmt.initializer != null)
        {
            Resolve(stmt.initializer);
        }
        Define(stmt.name);
        return null;
    }

    public VOID? VisitWhile(While stmt)
    {
        Resolve(stmt.condition);
        Resolve(stmt.body);
        return null;
    }

    public void Resolve(List<Stmt> statements)
    {
        foreach (Stmt statement in statements)
        {
            Resolve(statement);
        }
    }
    
    private void Resolve(Stmt statement)
    {
        statement.Accept(this);
    }
    
    private void Resolve(Expr expression)
    {
        expression.Accept(this);
    }
    
    private void BeginScope()
    {
        _scopes.Add(new Dictionary<string, bool>());
    }
    
    private void EndScope()
    {
        _scopes.RemoveAt(_scopes.Count - 1);
    }
    
    private void Declare(Token name)
    {
        if (_scopes.Count == 0) return;
        var scope = _scopes.Last();
        if (scope.ContainsKey(name.Lexeme))
        { 
            Lox.Error(name, "Already a variable with this name in this scope.");
        }
        scope[name.Lexeme] = false;
    }
    
    private void Define(Token name)
    {
        if (_scopes.Count == 0) return;
        _scopes.Last()[name.Lexeme] = true;
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, _scopes.Count - 1 - i);
                return;
            }
        }
    }
    
    private void ResolveFunction(Function stmt, FunctionType type)
    {
        FunctionType enclosingFunction = _currentFunction; 
        _currentFunction = type;
        BeginScope();
        foreach (Token param in stmt.parameters)
        {
            Declare(param);
            Define(param);
        }
        Resolve(stmt.body);
        EndScope();
        _currentFunction = enclosingFunction;
    }
    
    private enum FunctionType
    {
        NONE,
        FUNCTION,
        METHOD,
        INITIALIZER,
    }
    
    private enum ClassType
    {
        NONE,
        CLASS,
        SUBCLASS,
    }
} 