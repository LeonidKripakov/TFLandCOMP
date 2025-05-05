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
        // Флаг, указывающий, что обнаружены недопустимые символы (например, в ключевом слове)
        public bool InvalidKeyword { get; set; } = false;
        // Строка с недопустимыми символами
        public string InvalidCharacters { get; set; } = "";
        // Флаг, сигнализирующий об отсутствии разделяющего пробела (например, между "const" и именем)
        public bool MissingSpace { get; set; } = false;

        public Token(TokenType type, string value, int startIndex)
        {
            Type = type;
            Value = value;
            StartIndex = startIndex;
        }
    }

    public enum TokenType
    {
        CONST,
        IDENTIFIER,
        COLON,
        TYPE,
        EQUAL,
        NUMBER,
        SEMICOLON,
        INVALID_IDENTIFIER,
        UNKNOWN
    }

}
