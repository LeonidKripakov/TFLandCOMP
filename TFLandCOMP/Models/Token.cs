using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFLandCOMP.Models
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int StartIndex { get; }

        public Token(TokenType type, string value, int startIndex)
        {
            Type = type;
            Value = value;
            StartIndex = startIndex;
        }
    }

    public enum TokenType
    {
        IDENTIFIER,
        NUMBER,
        PLUS,
        MINUS,
        STAR,
        SLASH,
        LPAREN,
        RPAREN,
        UNKNOWN
    }
}
