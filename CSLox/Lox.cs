using System.Text;
using CSLox.Interpret;
using CSLox.Parse;
using CSLox.Resolve;
using CSLox.Scan;

namespace CSLox;

internal abstract class Lox
{
    private static readonly Interpreter Interpreter = new();
    private static bool _hadError;
    private static bool _hadRuntimeError;

    private static void Main(string[] args)
    {
#if DEBUG
        RunFile("../../../../CSLox/lox/main.lox");
#endif
        if (args.Length > 1)
        {
            Console.WriteLine("Usage: cslox [script]");
            System.Environment.Exit(64);
        }
    }

    private static void RunFile(String path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Run(Encoding.Default.GetString(bytes));
            
            if(_hadError) System.Environment.Exit(65);
            if (_hadRuntimeError) System.Environment.Exit(70); 
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static void RunPrompt()
    {
        try
        {
            while (true)
            {
                Console.WriteLine(">");
                var line = Console.ReadLine();
                // when user input ctrl+D, line will be null
                if (line == null) break;
                Run(line);
                _hadError = false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static void Run(string source)
    {
        Scanner scanner = new Scanner(source);
        List<Token> tokens = scanner.ScanTokens();
        
        Parser parser = new Parser(tokens);
        var stmts = parser.Parse(); 
        
        // Stop if there was a syntax error. 
        if (_hadError) return;
        
        Resolver resolver = new Resolver(Interpreter);
        resolver.Resolve(stmts);
        
        if (_hadError) return;
        Interpreter.Interpret(stmts);
    }
    
    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }
    
    public static void Error(Token token, String message) 
    { 
        if (token.Type == TokenType.EOF) 
        { 
            Report(token.Line, " at end", message); 
        } 
        else { 
            Report(token.Line, " at '" + token.Lexeme + "'",
                message); 
        }
    }
    
    private static void Report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error{where}: {message}");
        _hadError = true;
    }

    public static void RuntimeError(RuntimeError error) 
    { 
        Console.WriteLine(error.Message + "\n[line " + error.Token.Line + "]");
        _hadRuntimeError = true;
    }
}