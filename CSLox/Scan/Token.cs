namespace CSLox.Scan;

public enum TokenType 
{
    // Single-character tokens.
    LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
    COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR,
    // One or two character tokens.
    BANG, BANG_EQUAL,
    EQUAL, EQUAL_EQUAL,
    GREATER, GREATER_EQUAL,
    LESS, LESS_EQUAL,
    // Literals.
    IDENTIFIER, STRING, NUMBER,
    // Keywords.
    AND, CLASS, ELSE, FALSE, FUN, FOR, IF, NIL, OR,
    PRINT, RETURN, SUPER, THIS, TRUE, VAR, WHILE,
    
    EOF
}

class Token(TokenType type, string lexeme, object? literal, int line)
{
    public readonly TokenType Type = type;
    public readonly string Lexeme = lexeme;
    public readonly object? Literal = literal;
    public readonly int Line = line;
    public new string ToString => $"{Type} {Lexeme} {Literal}";
}