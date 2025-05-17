using CSLox.Scan;

namespace CSLox.Parse;

class Parser
{
    private int _current;
    private List<Token> _tokens;
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }
    // 我们先进的属性已经完全超越了老式的取值方法
    private Token Previous => _tokens[_current - 1];
    private bool IsAtEnd => Peek.Type == TokenType.EOF;
    private Token Peek => _tokens[_current];

    public List<Stmt> Parse()
    {
        try
        {
            List<Stmt> statements = new(); 
            while (!IsAtEnd) 
            { 
                statements.Add(Declaration());
            } 
            return statements;
        }
        catch (ParseError)
        {
            return null;
        }
    }

    #region Statement
    
    private Stmt Declaration()
    {
        try
        {
            if (Match(TokenType.CLASS)) return ClassDeclaration();
            if (Match(TokenType.FUN)) return Function("function");
            if (Match(TokenType.VAR)) return VarDeclaration();
            return Statement();
        }
        catch (ParseError)
        {
            Synchronize();
            return null;
        }
    }
    
    private Stmt ClassDeclaration()
    {
        Token name = Consume(TokenType.IDENTIFIER, "Expect class name.");
        Variable superclass = null;
        if (Match(TokenType.LESS))
        {
            Consume(TokenType.IDENTIFIER, "Expect superclass name.");
            superclass = new Variable(Previous);
        }
        Consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");
        List<Function> methods = new();
        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd)
        {
            methods.Add(Function("method"));
        }
        
        Consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");
        return new Class(name, superclass, methods);
    }
    
    private Stmt Statement() 
    {
        if (Match(TokenType.FOR)) 
            return ForStatement(); 
        if (Match(TokenType.IF)) 
            return IfStatement();
        if (Match(TokenType.PRINT))
            return PrintStatement();
        if (Match(TokenType.RETURN))
            return ReturnStatement();
        if (Match(TokenType.WHILE)) 
            return WhileStatement();
        if (Match(TokenType.LEFT_BRACE))
            return new Block(Block());
        return ExpressionStatement(); 
    }
    
    private Stmt ReturnStatement()
    {
        Token keyword = Previous;
        Expr value = null;
        if (!Check(TokenType.SEMICOLON))
        {
            value = Expression();
        }
        Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
        return new Return(keyword, value);
    }
    
    private Stmt ForStatement() 
    {
        // in fact, for loop is a sugar syntax for while loop
        // so we don't add a new class for it, we transform it to a while loop
        // like this:
        //
        // var i = 0; 
        // while (i < 10) { 
        //     print i; 
        //     i = i + 1; 
        // } 
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");
        Stmt initializer = null;
        if (Match(TokenType.SEMICOLON)) 
            ;
        else if (Match(TokenType.VAR))
            initializer = VarDeclaration();
        else
            initializer = ExpressionStatement();
        
        Expr condition = null; 
        if (!Check(TokenType.SEMICOLON))
        {
            condition = Expression();
        }
        Consume(TokenType.SEMICOLON, "Expect ';' after loop condition."); 
        
        Expr increment = null;
        if (!Check(TokenType.RIGHT_PAREN))
        {
            increment = Expression();
        }
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses."); 
        
        Stmt body = Statement();
        if (increment != null) 
            body = new Block([body, new Expression(increment)]);
        
        if (condition == null)
            condition = new Literal(true);
        body = new While(condition, body);
        
        if (initializer != null)
            body = new Block([initializer, body]); 
        
        return body;
    }
    
    private Stmt WhileStatement()
    {
        // todo : break, continue
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
        Stmt body = Statement();
        return new While(condition, body);
    }

    private Stmt IfStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
        Stmt thenBranch = Statement();
        Stmt elseBranch = null;
        if (Match(TokenType.ELSE))
        {
            elseBranch = Statement();
        }

        return new If(condition, thenBranch, elseBranch);
    }
    
    private Function Function(String kind)
    {
        Token name = Consume(TokenType.IDENTIFIER, $"Expect {kind} name.");
        Consume(TokenType.LEFT_PAREN, $"Expect '(' after {kind} name.");
        List<Token> parameters = new();
        if (!Check(TokenType.RIGHT_PAREN))
        {
            do
            {
                if (parameters.Count >= 255)
                {
                    Error(Peek, "Can't have more than 255 parameters.");
                }
                parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
            } while (Match(TokenType.COMMA));
        }
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");
        Consume(TokenType.LEFT_BRACE, $"Expect '{{' before {kind} body.");
        List<Stmt> body = Block();
        return new Function(name, parameters, body);
    }

    private Stmt VarDeclaration()
    { 
        Token name = Consume(TokenType.IDENTIFIER, "Expect variable name."); 
        Expr initializer = null;
        if (Match(TokenType.EQUAL))
        {
            initializer = Expression();
        }
        Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
        return new Var(name, initializer);
    }
    
    private List<Stmt> Block()
    {
        List<Stmt> statements = new();
        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd)
        {
            statements.Add(Declaration());
        }
        Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }
    
    private Stmt PrintStatement() 
    { 
        Expr value = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after value."); 
        return new Print(value); 
    }
    
    private Stmt ExpressionStatement() 
    { 
        Expr expr = Expression(); 
        Consume(TokenType.SEMICOLON, "Expect ';' after expression."); 
        return new Expression(expr); 
    } 

    #endregion

    #region Expression

    private Expr Expression()
    {
        return Assignment();
    }
    
    private Expr Assignment() 
    {
        Expr expr = Or();
        if (Match(TokenType.EQUAL))
        {
            Token equals = Previous;
            Expr value = Assignment();
            if (expr is Variable variable)
            {
                Token name = variable.name;
                return new Assign(name, value);
            }
            else if (expr is Get get)
            {
                return new Set(get.exprObject, get.name, value);
            }
            // We report but don't throw the error because the parser isn’t in
            // a confused state where we need to go into panic mode and synchronize.
            Error(equals, "Invalid assignment target."); 
        }
        return expr;
    }
    
    private Expr Or()
    {
        Expr expr = And();
        while (Match(TokenType.OR))
        {
            Token operatorToken = Previous;
            Expr right = And();
            expr = new Logical(expr, operatorToken, right);
        }
        return expr;
    }
    
    private Expr And()
    {
        Expr expr = Equality();
        while (Match(TokenType.AND))
        {
            Token operatorToken = Previous;
            Expr right = Equality();
            expr = new Logical(expr, operatorToken, right);
        }
        return expr;
    }
    
    private Expr Equality()
    { 
        Expr expr = Comparison();
        while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
        {
            Token operatorToken = Previous;
            Expr right = Comparison();
            expr = new Binary(expr, operatorToken, right);
        }
        return expr;
    }
    
    private Expr Comparison() 
    { 
        Expr expr = Term(); 
        while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL)) 
        { 
            Token operatorToken = Previous; 
            Expr right = Term(); 
            expr = new Binary(expr, operatorToken, right);
        } 
        return expr; 
    }
    
    private Expr Term()
    {
        Expr expr = Factor();
        while (Match(TokenType.MINUS, TokenType.PLUS))
        {
            Token operatorToken = Previous;
            Expr right = Factor();
            expr = new Binary(expr, operatorToken, right);
        }
        return expr;
    }

    private Expr Factor()
    {
        Expr expr = Unary();
        while (Match(TokenType.SLASH, TokenType.STAR))
        {
            Token operatorToken = Previous;
            Expr right = Unary();
            expr = new Binary(expr, operatorToken, right);
        }
        return expr;
    }

    private Expr Unary() 
    { 
        if (Match(TokenType.BANG, TokenType.MINUS))
        { 
            Token operatorToken = Previous; 
            Expr right = Unary(); 
            return new Unary(operatorToken, right); 
        } 
        return Call();
    }

    private Expr Call()
    {
        Expr expr = Primary();
        while (true)
        {
            if (Match(TokenType.LEFT_PAREN))
            {
                expr = FinishCall(expr);
            }
            else if (Match(TokenType.DOT))
            {
                Token name = Consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
                expr = new Get(expr, name);
            }
            else
            {
                break;
            }
        }
        return expr;
    }

    private Expr FinishCall(Expr callee)
    {
        List<Expr> arguments = new();
        if (!Check(TokenType.RIGHT_PAREN))
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    Error(Peek, "Can't have more than 255 arguments."); 
                }
                arguments.Add(Expression());
            } while (Match(TokenType.COMMA));
        }
        Token paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
        // we store the right parenthesis token to provide information for runtime errors
        return new Call(callee, paren, arguments);
    }

    private Expr Primary()
    { 
        if (Match(TokenType.FALSE)) return new Literal(false); 
        if (Match(TokenType.TRUE)) return new Literal(true); 
        if (Match(TokenType.NIL)) return new Literal(null); 
        
        if (Match(TokenType.NUMBER, TokenType.STRING)) 
            return new Literal(Previous.Literal);
        
        if (Match(TokenType.SUPER))
        {
            Token keyword = Previous;
            Consume(TokenType.DOT, "Expect '.' after 'super'.");
            Token method = Consume(TokenType.IDENTIFIER, "Expect superclass method name.");
            return new Super(keyword, method);
        }
        
        if (Match(TokenType.THIS)) return new This(Previous);
        
        if (Match(TokenType.IDENTIFIER)) 
        { 
            return new Variable(Previous); 
        } 
        
        if (Match(TokenType.LEFT_PAREN)) 
        {
            Expr expr = Expression(); 
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression."); 
            return new Grouping(expr);
        }
        throw Error(Peek, "Expect expression.");
    }

    #endregion

    #region Helper

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }
    
    private bool Check(TokenType type) 
    { 
        if (IsAtEnd) return false;
        return Peek.Type == type;
    }
    
    private Token Advance()
    {
        if (!IsAtEnd) _current++;
        return Previous;
    }
    
    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw Error(Peek, message);
    }

    #endregion

    #region ParseError

    private ParseError Error(Token token, string message) 
    { 
        Lox.Error(token, message);
        return new ParseError(message);
    }
    
    private void Synchronize()
    { 
        Advance(); 
        while (!IsAtEnd)
        { 
            if (Previous.Type == TokenType.SEMICOLON) return; 
            switch (Peek.Type)
            { 
                case TokenType.CLASS: 
                case TokenType.FUN: 
                case TokenType.VAR: 
                case TokenType.FOR: 
                case TokenType.IF: 
                case TokenType.WHILE: 
                case TokenType.PRINT: 
                case TokenType.RETURN: 
                    return; 
            } 
            Advance(); 
        } 
    }
    #endregion
} 

class ParseError(string message) : Exception(message);