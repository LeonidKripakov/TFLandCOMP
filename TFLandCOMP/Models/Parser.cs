using System.Collections.Generic;

using TFLandCOMP.Models;

public class ExpressionParser
{
    private List<Token> tokens;
    private int pos;
    private List<ErrorDetail> errors;
    private string input;
    private int tempCounter;
    private List<Quadruple> quadruples;

    public List<ErrorDetail> Parse(List<Token> tokens, string input)
    {
        this.tokens = tokens;
        this.input = input;
        this.errors = new List<ErrorDetail>();
        this.pos = 0;
        this.tempCounter = 0;
        this.quadruples = new List<Quadruple>();

        try
        {
            ParseE();
        }
        catch { }

        if (pos < tokens.Count)
        {
            errors.Add(new ErrorDetail
            {
                ErrorCode = "E999",
                ErrorMessage = $"Неожиданный токен: '{tokens[pos].Value}'",
                Position = $"Позиция: {tokens[pos].StartIndex}"
            });
        }

        return errors;
    }

    public List<Quadruple> Generate(List<Token> tokens)
    {
        this.tokens = tokens;
        this.pos = 0;
        this.tempCounter = 0;
        this.quadruples = new List<Quadruple>();
        ParseE();
        return quadruples;
    }

    private string ParseE()
    {
        string left = ParseT();  // сначала T — с приоритетом (* /)
        while (Match(TokenType.PLUS) || Match(TokenType.MINUS))
        {
            string op = Advance().Value;
            string right = ParseT();
            string temp = NewTemp();
            quadruples.Add(new Quadruple
            {
                Operation = op,
                Arg1 = left,
                Arg2 = right,
                Result = temp
            });
            left = temp;
        }
        return left;
    }


    private string ParseT()
    {
        string left = ParseO(); // самый базовый элемент (id или (E))
        while (Match(TokenType.STAR) || Match(TokenType.SLASH))
        {
            string op = Advance().Value;
            string right = ParseO();
            string temp = NewTemp();
            quadruples.Add(new Quadruple
            {
                Operation = op,
                Arg1 = left,
                Arg2 = right,
                Result = temp
            });
            left = temp;
        }
        return left;
    }


    private string ParseO()
    {
        if (Match(TokenType.IDENTIFIER))
        {
            return Advance().Value;
        }
        else if (Match(TokenType.LPAREN))
        {
            Advance(); // пропускаем (
            string val = ParseE();
            if (!Match(TokenType.RPAREN))
            {
                errors.Add(new ErrorDetail
                {
                    ErrorCode = "E002",
                    ErrorMessage = "Пропущена закрывающая скобка ')'",
                    Position = $"Позиция: {CurrentIndex()}"
                });
            }
            else
            {
                Advance(); // пропускаем )
            }
            return val;
        }
        else
        {
            errors.Add(new ErrorDetail
            {
                ErrorCode = "E001",
                ErrorMessage = $"Ожидался идентификатор или '('",
                Position = $"Позиция: {CurrentIndex()}"
            });
            Advance();
            return "ERR";
        }
    }

    private bool Match(TokenType type) => pos < tokens.Count && tokens[pos].Type == type;

    private Token Advance() => tokens[pos++];

    private int CurrentIndex() => pos < tokens.Count ? tokens[pos].StartIndex : input.Length;

    private string NewTemp() => $"t{tempCounter++}";
}
