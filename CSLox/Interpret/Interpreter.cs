using CSLox.Parse;
using CSLox.Scan;

namespace CSLox.Interpret;

class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<VOID?>
{
    private readonly Environment _globals = new();
    private readonly Dictionary<Expr, int> _locals = new();
    private Environment _environment; 
    public Interpreter()
    {
        _environment = _globals;
        _globals.Define("clock", new LoxClock());
    }
    public void Interpret(List<Stmt> statements) 
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }

    #region Stmt
    
    private void Execute(Stmt stmt) 
    { 
        stmt.Accept(this); 
    }

    public VOID? VisitBlock(Block stmt)
    {
        ExecuteBlock(stmt.statements, new Environment(_environment)); 
        return null; 
    }

    public VOID? VisitClass(Class stmt)
    {
        object superclass = null;
        if (stmt.superclass != null)
        {
            superclass = Evaluate(stmt.superclass);
            if (superclass is not LoxClass)
            {
                throw new RuntimeError(stmt.superclass.name, "Superclass must be a class.");
            }
        }
        
        _environment.Define(stmt.name.Lexeme, null);
        
        if (stmt.superclass != null) 
        {
            _environment = new Environment(_environment);
            _environment.Define("super", superclass);
        }
        
        Dictionary<string, LoxFunction> methods = new();
        foreach (Function method in stmt.methods)
        {
            LoxFunction function = new LoxFunction(method, _environment, method.name.Lexeme.Equals("init"));
            methods[method.name.Lexeme] = function;
        }
        
        LoxClass klass = new LoxClass(stmt.name.Lexeme, (LoxClass)superclass, methods);
        
        if (superclass != null)
        {
            _environment = _environment.Enclosing;
        }
        
        _environment.Assign(stmt.name, klass);
        return null;
    }

    public VOID? VisitExpression(Expression stmt)
    {
        Evaluate(stmt.expression); 
        return null;
    }

    public VOID? VisitFunction(Function stmt)
    {
        var function = new LoxFunction(stmt, _environment, false);
        _environment.Define(stmt.name.Lexeme, function);
        return null;
    }

    public VOID? VisitIf(If stmt)
    {
        if (IsTruthy(Evaluate(stmt.condition))) 
        { 
            Execute(stmt.thenBranch); 
        } 
        else if (stmt.elseBranch != null) 
        {
            Execute(stmt.elseBranch); 
        } 
        return null;
    }

    public VOID? VisitPrint(Print stmt)
    {
        Object value = Evaluate(stmt.expression); 
        Console.WriteLine(Stringify(value));
        return null; 
    }

    public VOID? VisitReturn(CSLox.Parse.Return stmt)
    {
        object value = null;
        if (stmt.value != null) 
        {
            value = Evaluate(stmt.value); 
        }
        throw new Return(value);
    }

    public VOID? VisitVar(Var stmt)
    {
        object value = null;
        if (stmt.initializer != null) 
        {
            value = Evaluate(stmt.initializer); 
        }
        _environment.Define(stmt.name.Lexeme, value);
        return null;
    }

    public VOID? VisitWhile(While stmt)
    {
        while (IsTruthy(Evaluate(stmt.condition)))
        {
            Execute(stmt.body);
        }
        return null;
    }

    public void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment previous = _environment;
        try
        {
            _environment = environment;
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
    }

    #endregion
    
    #region Expr
    
    private object Evaluate(Expr expr) 
    { 
        return expr.Accept(this); 
    }

    public object VisitAssign(Assign expr)
    {
        var value = Evaluate(expr.value);
        int? distance = null;
        if (_locals.TryGetValue(expr, out var d))
        {
            distance = d;
        }
        if (distance != null)
        {
            _environment.AssignAt(distance.Value, expr.name, value);
        }
        else
        {
            _globals.Assign(expr.name, value); 
        }
        return value;
    }

    public object VisitBinary(Binary expr)
    {
        var left = Evaluate(expr.left);
        var right = Evaluate(expr.right);
 
        switch (expr.operatorToken.Type) 
        { 
            case TokenType.MINUS: 
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left - (double)right; 
            case TokenType.SLASH: 
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left / (double)right; 
            case TokenType.STAR: 
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left * (double)right;
            case TokenType.PLUS: 
                if (left is double && right is double) 
                { 
                    return (double)left + (double)right; 
                }
                if (left is string && right is string) 
                {
                    return (string)left + (string)right; 
                } 
                throw new RuntimeError(expr.operatorToken, "Operands must be two numbers or two strings."); 
            case TokenType.GREATER:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left > (double)right; 
            case TokenType.GREATER_EQUAL:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left >= (double)right; 
            case TokenType.LESS: 
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left < (double)right; 
            case TokenType.LESS_EQUAL: 
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left <= (double)right;
            case TokenType.BANG_EQUAL: return !IsEqual(left, right); 
            case TokenType.EQUAL_EQUAL: return IsEqual(left, right); 
        } 
 
        // Unreachable.
        return null;
    }

    public object VisitCall(Call expr)
    {
        object callee = Evaluate(expr.callee);
        
        List<object> arguments = new();
        foreach (Expr argument in expr.arguments)
        {
            arguments.Add(Evaluate(argument));
        }
        if(callee is not ILoxCallAble function)
        {
            throw new RuntimeError(expr.paren, "Can only call functions and classes.");
        }
        if(arguments.Count != function.Arity)
        {
            throw new RuntimeError(expr.paren, $"Expected {function.Arity} arguments but got {arguments.Count}.");
        }

        return function.Call(this, arguments);
    }

    public object VisitGet(Get expr)
    {
        object exprObject = Evaluate(expr.exprObject);
        if (exprObject is LoxInstance instance) 
        {
            return instance.Get(expr.name);
        }

        throw new RuntimeError(expr.name, "Only instances have properties.");
    }

    public object VisitGrouping(Grouping expr)
        {
            return Evaluate(expr.expression);
        }

        public object VisitLiteral(Literal expr)
        {
            return expr.value;
        }

        public object VisitLogical(Logical expr)
        {
            object left = Evaluate(expr.left);
            if (expr.operatorToken.Type == TokenType.OR)
            {
                if (IsTruthy(left)) return left;
            }
            else
            {
                if (!IsTruthy(left)) return left;
            }
            return Evaluate(expr.right);
        }

        public object VisitSet(Set expr)
        {
            object exprObject = Evaluate(expr.exprObject);
            if (!(exprObject is LoxInstance instance))
            {
                throw new RuntimeError(expr.name, "Only instances have fields.");
            }
            object value = Evaluate(expr.value);
            instance.Set(expr.name, value);
            return value;
        }

        public object VisitSuper(Super expr)
        {
            int distance = _locals[expr];
            LoxClass superclass = (LoxClass)_environment.GetAt(distance, "super");
            
            // Even we are calling the method in the superclass, "this" is still the instance of the subclass.
            LoxInstance instance = (LoxInstance)_environment.GetAt(distance - 1, "this");
            LoxFunction method = superclass.FindMethod(expr.method.Lexeme);
            
            if (method == null)
            {
                throw new RuntimeError(expr.method, "Undefined property '" + expr.method.Lexeme + "'.");
            }
            
            return method.Bind(instance);
        }

        public object VisitThis(This expr)
        {
            return LookUpVariable(expr.keyword, expr);
        }

        public object VisitUnary(Unary expr)
        {
            object right = Evaluate(expr.right); 
            switch (expr.operatorToken.Type) 
            { 
                case TokenType.MINUS:
                    CheckNumberOperand(expr.operatorToken, right); 
                    return -(double)right; 
                case TokenType.BANG: 
                    return !IsTruthy(right); 
            }
            return null;
        }

        public object VisitVariable(Variable expr)
        {
            return LookUpVariable(expr.name, expr);
        }

        private object LookUpVariable(Token name, Expr expr)
        {
            int? distance = null;
            if (_locals.TryGetValue(expr, out var d))
            {
                distance = d;
            }
            if (distance != null)
            {
                return _environment.GetAt(distance.Value, name.Lexeme);
            }
            else
            {
                return _globals.Get(name);
            }
        }

        #endregion

        #region Helper

        private string Stringify(object value) 
        { 
            if (value == null) return "nil"; 
            if (value is Double) 
            { 
                string text = value.ToString(); 
                if (text.EndsWith(".0")) 
                { 
                    text = text.Substring(0, text.Length - 2); 
                } 
                return text; 
            } 
            return value.ToString(); 
        }
    
        private void CheckNumberOperand(Token operatorToken, object operand)
        { 
            if (operand is double) return; 
            throw new RuntimeError(operatorToken, "Operand must be a number."); 
        } 
    
        private void CheckNumberOperands(Token operatorToken,object left, object right) 
        { 
            if (left is double && right is double)return;
            throw new RuntimeError(operatorToken,"Operands must be numbers."); 
        }
    
        private bool IsEqual(object a, object b)
        {
            if (a == null && b == null) return true; 
            if (a == null) return false;
            return a.Equals(b);
        }
    
        private bool IsTruthy(object value)
        { 
            if (value == null) return false; 
            if (value is bool b) return b; 
            return true; 
        }

        #endregion
    
        public void Resolve(Expr expr, int depth) 
        { 
            _locals[expr] = depth;
        }
}

/// <summary>
/// Unlike java, in C#, Void can not be used as a generic type.
/// </summary>
public struct VOID;