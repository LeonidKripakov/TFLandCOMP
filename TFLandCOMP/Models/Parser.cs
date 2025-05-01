using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TFLandCOMP.Models
{
    public class Parser
    {
        private HashSet<string> declaredVariables = new HashSet<string>();

        /// <summary>
        /// Главный метод синтаксического анализа. Принимает список токенов и исходный текст,
        /// возвращает список ошибок.
        /// </summary>
        public List<ErrorDetail> ParseDeclarations(List<Token> tokens, string input)
        {
            List<ErrorDetail> errors = new List<ErrorDetail>();
            int pos = 0;
            while (pos < tokens.Count)
            {
                if (tokens[pos].Type != TokenType.CONST)
                {
                    errors.Add(new ErrorDetail
                    {
                        ErrorCode = "E001",
                        ErrorMessage = $"Ожидалось ключевое слово 'const', но найдено '{tokens[pos].Value}'.",
                        Position = GetLineAndColumn(input, tokens[pos].StartIndex)
                    });

                    // Вместо пропуска до ';' — обрабатываем как потенциальное объявление
                    var declErrors = ParseSingleDeclaration(tokens, ref pos, input);
                    errors.AddRange(declErrors);
                }


                else
                {
                    var declErrors = ParseSingleDeclaration(tokens, ref pos, input);
                    errors.AddRange(declErrors);
                }
            }
            return errors;
        }

        private List<ErrorDetail> ParseSingleDeclaration(List<Token> tokens, ref int pos, string input)
        {
            List<ErrorDetail> errors = new List<ErrorDetail>();

            void AddError(string code, string message, int tokenPos)
            {
                errors.Add(new ErrorDetail
                {
                    ErrorCode = code,
                    ErrorMessage = message,
                    Position = GetLineAndColumn(input, tokenPos)
                });
            }

            Token constToken = tokens[pos];
            // Проверяем флаги для ключевого слова "const"
            if (constToken.MissingSpace)
            {
                AddError("E010", "Отсутствует разделяющий пробел между ключевым словом 'const' и именем переменной.", constToken.StartIndex);
            }
            if (constToken.InvalidKeyword)
            {
                AddError("E011", $"Некорректное написание ключевого слова 'const': обнаружены недопустимые символы: '{constToken.InvalidCharacters}'", constToken.StartIndex);
            }
            pos++;

            // Ожидаем идентификатор переменной
            if (pos < tokens.Count && tokens[pos].Type == TokenType.IDENTIFIER)
            {
                var varName = tokens[pos].Value;
                if (declaredVariables.Contains(varName))
                {
                    AddError("E008", "Такая переменная уже существует.", tokens[pos].StartIndex);
                }
                else
                {
                    declaredVariables.Add(varName);
                }
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E002", "Ожидалось имя константы (идентификатор).", tokens[pos].StartIndex);
                else
                    AddError("E002", "Ожидалось имя константы (идентификатор), но не найдено.", input.Length);
                pos++;
            }

            // Ожидаем символ ':'
            if (pos < tokens.Count && tokens[pos].Type == TokenType.COLON)
            {
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E003", "Ожидался символ ':' после имени константы.", tokens[pos].StartIndex);
                else
                    AddError("E003", "Ожидался символ ':' после имени константы, но не найден.", input.Length);
                pos++;
            }

            // Ожидаем тип константы (например, f32 или f64)
            if (pos < tokens.Count && tokens[pos].Type == TokenType.TYPE)
            {
                if (tokens[pos].InvalidKeyword || tokens[pos].MissingSpace)
                {
                    AddError("E004", $"Ожидался тип константы (например, f32 или f64): обнаружены недопустимые символы: '{tokens[pos].InvalidCharacters}'", tokens[pos].StartIndex);
                }
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E004", "Ожидался тип константы (например, f32 или f64).", tokens[pos].StartIndex);
                else
                    AddError("E004", "Ожидался тип константы (например, f32 или f64), но не найден.", input.Length);
                pos++;
            }

                // --- НОВАЯ ПРОВЕРКА: если после типа встретился идентификатор, а не '=' или число, ругаемся один раз и пропускаем его ---
                if (pos < tokens.Count && tokens[pos].Type == TokenType.IDENTIFIER)
                    {
                AddError("E012", $"Некорректный токен '{tokens[pos].Value}' после типа: ожидался символ '=' или числовой литерал.", tokens[pos].StartIndex);
                pos++;  // пропускаем «лишний» идентификатор
                    }

            // Ожидаем символ '='
            if (pos < tokens.Count && tokens[pos].Type == TokenType.EQUAL)
            {
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E005", "Ожидался символ '=' после типа.", tokens[pos].StartIndex);
                else
                    AddError("E005", "Ожидался символ '=' после типа, но не найден.", input.Length);
                pos++;
            }

            // Ожидаем числовой литерал
            if (pos < tokens.Count && tokens[pos].Type == TokenType.NUMBER)
            {
                // … ваша проверка формата числа …
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E006", "Ожидалось числовое значение для инициализации.", tokens[pos].StartIndex);
                else
                    AddError("E006", "Ожидалось числовое значение для инициализации, но не найдено.", input.Length);
                pos++;
            }
            // Ожидаем символ ';'
            if (pos < tokens.Count && tokens[pos].Type == TokenType.SEMICOLON)
            {
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E007", "Ожидался символ ';' в конце объявления, но не найден.", tokens[pos].StartIndex);
                else
                    AddError("E007", "Ожидался символ ';' в конце объявления, но не найден.", input.Length);
                while (pos < tokens.Count && tokens[pos].Type != TokenType.CONST)
                    pos++;
            }
            return errors;
        }

        /// <summary>
        /// Получает строку с номером строки и столбца для заданного индекса в тексте.
        /// </summary>
        private string GetLineAndColumn(string text, int index)
        {
            int line = 1, col = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    col = 1;
                }
                else
                {
                    col++;
                }
            }
            return $"Строка {line}, Колонка {col}";
        }
    }
}
