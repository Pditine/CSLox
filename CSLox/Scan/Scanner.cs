namespace CSLox.Scan;

class Scanner
{
    private readonly String _source;
    private readonly List<Token> _tokens = new();
    private static readonly Dictionary<String, TokenType> Keywords = new()
    {
        { "and", TokenType.AND },
        { "class", TokenType.CLASS },
        { "else", TokenType.ELSE },
        { "false", TokenType.FALSE },
        { "for", TokenType.FOR },
        { "fun", TokenType.FUN },
        { "if", TokenType.IF },
        { "nil", TokenType.NIL },
        { "or", TokenType.OR },
        { "print", TokenType.PRINT },
        { "return", TokenType.RETURN },
        { "super", TokenType.SUPER },
        { "this", TokenType.THIS },
        { "true", TokenType.TRUE },
        { "var", TokenType.VAR },
        { "while", TokenType.WHILE },
    };
    
    private int _start;
    private int _current;
    private int _line = 1;
    
    private bool IsAtEnd => _current >= _source.Length;
    
    public Scanner(String source) 
    { 
        _source = source; 
    }
    
    public List<Token> ScanTokens() 
    { 
        while (!IsAtEnd) 
        { 
            _start = _current; 
            ScanToken(); 
        } 
        _tokens.Add(new Token(TokenType.EOF, "", null, _line));
        return _tokens; 
    }
    
    private void ScanToken()
    { 
        char c = Advance(); 
        switch (c) 
        { 
            case '(': AddToken(TokenType.LEFT_PAREN); break; 
            case ')': AddToken(TokenType.RIGHT_PAREN); break; 
            case '{': AddToken(TokenType.LEFT_BRACE); break; 
            case '}': AddToken(TokenType.RIGHT_BRACE); break; 
            case ',': AddToken(TokenType.COMMA); break; 
            case '.': AddToken(TokenType.DOT); break; 
            case '-': AddToken(TokenType.MINUS); break; 
            case '+': AddToken(TokenType.PLUS); break; 
            case ';': AddToken(TokenType.SEMICOLON); break; 
            case '*': AddToken(TokenType.STAR); break; 
            case '!': AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break; 
            case '=': AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL); break; 
            case '<': AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break; 
            case '>': AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break; 
            case '/': 
                if (Match('/')) 
                { 
                    // we don't call AddToken here because we want to ignore the rest of the line
                    while (Peek() != '\n' && !IsAtEnd) Advance(); 
                } 
                else 
                { 
                    AddToken(TokenType.SLASH); 
                } 
                break; 
            // omit white space
            case ' ':
            case '\r':
            case '\t':
                break;
            case '\n':
                _line++; 
                break; 
            case '"': AddString(); break; 
            default: 
                if (IsDigit(c))
                { 
                    AddNumber(); 
                } 
                else if (IsAlpha(c)) 
                {
                    AddIdentifier();
                } 
                else 
                { 
                    Lox.Error(_line, $"Unexpected character.{c}"); 
                }
                break; 
        } 
    } 
    
    private void AddNumber()
    {
        while (IsDigit(Peek())) Advance(); 
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance();
            while (IsDigit(Peek())) Advance();
        }
        // boxing，but we don't care about the performance here
        AddToken(TokenType.NUMBER, Double.Parse(_source.Substring(_start, _current - _start))); 
    }
    
    private void AddString() 
    { 
        while (Peek() != '"' && !IsAtEnd) 
        { 
            if (Peek() == '\n') _line++; 
            Advance(); 
        } 
        if (IsAtEnd)
        { 
            Lox.Error(_line, "Unterminated string."); 
            return; 
        } 
        Advance(); 
        String value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.STRING, value); 
    } 
    
    private void AddIdentifier() 
    { 
        while (IsAlphaNumeric(Peek())) Advance(); 
        String text = _source.Substring(_start, _current - _start);
        var type = Keywords.GetValueOrDefault(text, TokenType.IDENTIFIER);
        AddToken(type);
    }
    
    private bool IsAlphaNumeric(char c)
    { 
        return IsAlpha(c) || IsDigit(c); 
    } 
    
    private bool IsAlpha(char c) 
    { 
        return c is >= 'a' and <= 'z' || c is >= 'A' and <= 'Z' || c == '_'; 
    } 
    
    private bool IsDigit(char c)
    { 
        return c is >= '0' and <= '9'; 
    } 
    
    private bool Match(char expected) 
    { 
        if (IsAtEnd) return false; 
        if (_source[_current] != expected) return false; 
        _current++; 
        return true; 
    } 
    
    private char Peek() 
    { 
        if (IsAtEnd) return '\0'; 
        return _source[_current]; 
    } 
    
    private char PeekNext() 
    { 
        if (_current + 1 >= _source.Length) return '\0'; 
        return _source[_current + 1]; 
    } 
    
    private char Advance() 
    { 
        return _source[_current++]; 
    }

    private void AddToken(TokenType type, object? literal = null)
    { 
        String text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line));
    } 
}