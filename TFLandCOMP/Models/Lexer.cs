using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFLandCOMP.Models
{
    public class Lexer
    {
        /// <summary>
        /// Разбивает входной текст на токены.
        /// </summary>
        public List<Token> Lex(string input)
        {
            List<Token> tokens = new List<Token>();
            int i = 0;
            while (i < input.Length)
            {
                char c = input[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }
                // Если символ – буква, накапливаем до разделителя (пробел, ':', '=' или ';').
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < input.Length &&
                           !char.IsWhiteSpace(input[i]) &&
                           input[i] != ':' &&
                           input[i] != '=' &&
                           input[i] != ';')
                    {
                        i++;
                    }
                    string word = input.Substring(start, i - start);
                    // Извлекаем только буквы
                    string lettersOnly = new string(word.Where(ch => char.IsLetter(ch)).ToArray());
                    // Обработка ключевого слова "const"
                    if (lettersOnly.StartsWith("const"))
                    {
                        var result = ProcessKeyword(word, "const");
                        Token tokenConst = new Token(TokenType.CONST, "const", start)
                        {
                            MissingSpace = result.missingSpace,
                            InvalidKeyword = result.invalid,
                            InvalidCharacters = result.invalid ? result.invalidChars : ""
                        };
                        tokens.Add(tokenConst);
                        // Если обнаружено отсутствие пробела — остаток трактуем как идентификатор
                        if (result.missingSpace && !string.IsNullOrEmpty(result.remainder))
                        {
                            tokens.Add(new Token(TokenType.IDENTIFIER, result.remainder, start + result.keywordPart.Length));
                        }
                    }
                    // Обработка типа "f32"
                    else if (lettersOnly.StartsWith("f32") || word.StartsWith("f32"))
                    {
                        if (word == "f32")
                        {
                            tokens.Add(new Token(TokenType.TYPE, "f32", start));
                        }
                        else if (word.Length > "f32".Length && char.IsLetterOrDigit(word["f32".Length]))
                        {
                            Token tokenType = new Token(TokenType.TYPE, "f32", start)
                            {
                                MissingSpace = true,
                                InvalidKeyword = true,
                                InvalidCharacters = word.Substring("f32".Length)
                            };
                            tokens.Add(tokenType);
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.TYPE, "f32", start));
                        }
                    }
                    // Обработка типа "f64"
                    else if (lettersOnly.StartsWith("f64") || word.StartsWith("f64"))
                    {
                        if (word == "f64")
                        {
                            tokens.Add(new Token(TokenType.TYPE, "f64", start));
                        }
                        else if (word.Length > "f64".Length && char.IsLetterOrDigit(word["f64".Length]))
                        {
                            Token tokenType = new Token(TokenType.TYPE, "f64", start)
                            {
                                MissingSpace = true,
                                InvalidKeyword = true,
                                InvalidCharacters = word.Substring("f64".Length)
                            };
                            tokens.Add(tokenType);
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.TYPE, "f64", start));
                        }
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.IDENTIFIER, word, start));
                    }
                    continue;
                }
                // Если цифра, накапливаем до разделителя.
                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < input.Length &&
                           !char.IsWhiteSpace(input[i]) &&
                           input[i] != ':' &&
                           input[i] != '=' &&
                           input[i] != ';')
                    {
                        i++;
                    }
                    string number = input.Substring(start, i - start);
                    tokens.Add(new Token(TokenType.NUMBER, number, start));
                    continue;
                }
                // Обработка одиночных символов-разделителей.
                switch (c)
                {
                    case ':':
                        tokens.Add(new Token(TokenType.COLON, ":", i));
                        i++;
                        break;
                    case '=':
                        tokens.Add(new Token(TokenType.EQUAL, "=", i));
                        i++;
                        break;
                    case ';':
                        tokens.Add(new Token(TokenType.SEMICOLON, ";", i));
                        i++;
                        break;
                    default:
                        tokens.Add(new Token(TokenType.UNKNOWN, c.ToString(), i));
                        i++;
                        break;
                }
            }
            return tokens;
        }

        /// <summary>
        /// Обработка потенциального ключевого слова. Накопленный префикс сравнивается с ожидаемым.
        /// Если после удаления не-букв получаем нужное слово, возвращается:
        /// - keywordPart: накопленный префикс,
        /// - remainder: оставшаяся часть слова,
        /// - invalid: true, если накопленный префикс не совпадает с ожидаемым,
        /// - invalidChars: строка недопустимых символов в префиксе,
        /// - missingSpace: true, если после префикса осталась ненулевая часть.
        /// </summary>
        private (string keywordPart, string remainder, bool invalid, string invalidChars, bool missingSpace) ProcessKeyword(string word, string expected)
        {
            StringBuilder candidate = new StringBuilder();
            StringBuilder candidateClean = new StringBuilder();
            int pos = 0;
            for (; pos < word.Length; pos++)
            {
                char ch = word[pos];
                candidate.Append(ch);
                if (char.IsLetter(ch))
                    candidateClean.Append(ch);
                if (candidateClean.ToString() == expected)
                    break;
            }
            // Если не удалось набрать требуемое слово — считаем, что есть ошибка
            if (candidateClean.ToString() != expected)
            {
                return (word, "", true, new string(word.Where(ch => !char.IsLetter(ch)).ToArray()), false);
            }
            int splitPos = pos + 1; // позиция после накопленного префикса
            bool missingSpace = (splitPos < word.Length);
            string remainder = missingSpace ? word.Substring(splitPos) : "";
            bool invalid = candidate.ToString() != expected;
            string invalidChars = invalid ? new string(candidate.ToString().Where(ch => !char.IsLetter(ch)).ToArray()) : "";
            return (candidate.ToString(), remainder, invalid, invalidChars, missingSpace);
        }
    }
}
