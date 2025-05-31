using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TFLandCOMP.Models
{
    public class Lexer
    {
        public List<Token> Lex(string input)
        {
            var tokens = new List<Token>();
            int i = 0;
            while (i < input.Length)
            {
                char c = input[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < input.Length && char.IsLetterOrDigit(input[i]))
                        i++;

                    string word = input.Substring(start, i - start);
                    tokens.Add(new Token(TokenType.IDENTIFIER, word, start));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.'))
                        i++;

                    string number = input.Substring(start, i - start);
                    tokens.Add(new Token(TokenType.NUMBER, number, start));
                    continue;
                }

                switch (c)
                {
                    case '+':
                        tokens.Add(new Token(TokenType.PLUS, "+", i)); i++; break;
                    case '-':
                        tokens.Add(new Token(TokenType.MINUS, "-", i)); i++; break;
                    case '*':
                        tokens.Add(new Token(TokenType.STAR, "*", i)); i++; break;
                    case '/':
                        tokens.Add(new Token(TokenType.SLASH, "/", i)); i++; break;
                    case '(':
                        tokens.Add(new Token(TokenType.LPAREN, "(", i)); i++; break;
                    case ')':
                        tokens.Add(new Token(TokenType.RPAREN, ")", i)); i++; break;
                    default:
                        tokens.Add(new Token(TokenType.UNKNOWN, c.ToString(), i)); i++; break;
                }
            }

            return tokens;
        }
    }
}
