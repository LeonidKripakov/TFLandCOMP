using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TFLandCOMP.Models
{
    public class Parser
    {
        private readonly HashSet<string> declaredVariables = new HashSet<string>();

        // Шаблоны для проверки числа и кириллицы
        private static readonly Regex FloatRegex = new Regex(@"^[+-]?\d+\.\d+$", RegexOptions.Compiled);
        private static readonly Regex CyrillicRegex = new Regex(@"\p{IsCyrillic}", RegexOptions.Compiled);

        public List<ErrorDetail> ParseDeclarations(List<Token> tokens, string input)
        {
            var errors = new List<ErrorDetail>();

            // ── РАННЯЯ ВЕТКА: если первый токен не CONST, выдаём ровно три ошибки и выходим ──
            if (tokens.Count == 0 || tokens[0].Type != TokenType.CONST)
            {
                // 1) E001 – неверное начало
                if (tokens.Count > 0)
                {
                    errors.Add(MakeError("E001",
                        $"Ожидалось ключевое слово 'const', но найдено '{tokens[0].Value}'.",
                        input, tokens[0].StartIndex));
                }
                else
                {
                    errors.Add(MakeError("E001",
                        "Ожидалось ключевое слово 'const', но строка пуста.",
                        input, 0));
                }

                // 2) E004 – проверяем тип: ищем первое ':' и смотрим следующий токен
                int colonIdx = tokens.FindIndex(t => t.Type == TokenType.COLON);
                if (colonIdx >= 0 && colonIdx + 1 < tokens.Count)
                {
                    var typeToken = tokens[colonIdx + 1];
                    if (typeToken.Type != TokenType.TYPE)
                    {
                        errors.Add(MakeError("E004",
                            "Ожидался тип (f32 или f64).",
                            input, typeToken.StartIndex));
                    }
                }
                else
                {
                    // нет ':' или после него нет токена → тоже считаем, что тип отсутствует
                    errors.Add(MakeError("E004",
                        "Ожидался тип (f32 или f64).",
                        input, CurrentPos(tokens, tokens.Count)));
                }

                // 3) E013 – проверяем число: ищем первое '=' и смотрим следующий токен
                int eqIdx = tokens.FindIndex(t => t.Type == TokenType.EQUAL);
                if (eqIdx >= 0 && eqIdx + 1 < tokens.Count)
                {
                    var numToken = tokens[eqIdx + 1];
                    if (numToken.Type == TokenType.NUMBER)
                    {
                        if (CyrillicRegex.IsMatch(numToken.Value) || !FloatRegex.IsMatch(numToken.Value))
                        {
                            errors.Add(MakeError("E013",
                                $"Неверный формат числового литерала: '{numToken.Value}'.",
                                input, numToken.StartIndex));
                        }
                    }
                    else
                    {
                        // если после '=' что-то не NUMBER
                        errors.Add(MakeError("E006",
                            "Ожидалось числовое значение для инициализации.",
                            input, numToken.StartIndex));
                    }
                }
                else
                {
                    // нет '=' или числа после него
                    errors.Add(MakeError("E006",
                        "Ожидалось числовое значение для инициализации.",
                        input, CurrentPos(tokens, tokens.Count)));
                }

                return errors;
            }

            // ── ИНАЧЕ: стандартный однопроходный разбор полного объявления ──
            int pos = 0;
            while (pos < tokens.Count)
            {
                // Проверка CONST
                if (tokens[pos].Type != TokenType.CONST)
                {
                    errors.Add(MakeError("E001",
                        $"Ожидалось ключевое слово 'const', но найдено '{tokens[pos].Value}'.",
                        input, tokens[pos].StartIndex));
                }
                else
                {
                    // если это именно токен CONST, проверяем флаги
                    if (tokens[pos].MissingSpace)
                        errors.Add(MakeError("E010",
                            "Отсутствует пробел между 'const' и именем переменной.",
                            input, tokens[pos].StartIndex));
                    if (tokens[pos].InvalidKeyword)
                        errors.Add(MakeError("E011",
                            $"Некорректное написание 'const': '{tokens[pos].InvalidCharacters}'.",
                            input, tokens[pos].StartIndex));
                    pos++;
                }

                // Парсим «const Id : Type = Number ;»
                var declErrors = ParseSingleDeclaration(tokens, ref pos, input);
                errors.AddRange(declErrors);
            }

            return errors;
        }

        private List<ErrorDetail> ParseSingleDeclaration(List<Token> tokens, ref int pos, string input)
        {
            var errs = new List<ErrorDetail>();
            void Add(string code, string msg, int idx) =>
                errs.Add(MakeError(code, msg, input, idx));

            // Если после CONST нет больше токенов — неполное
            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление константы.", CurrentPos(tokens, pos));
                return errs;
            }

            // Идентификатор
            if (tokens[pos].Type == TokenType.IDENTIFIER ||
                tokens[pos].Type == TokenType.INVALID_IDENTIFIER)
            {
                if (tokens[pos].Type == TokenType.INVALID_IDENTIFIER)
                    Add("E014",
                        $"Недопустимые символы в имени: '{tokens[pos].Value}'.",
                        tokens[pos].StartIndex);
                else if (!declaredVariables.Add(tokens[pos].Value))
                    Add("E008",
                        "Такая переменная уже существует.",
                        tokens[pos].StartIndex);
                pos++;
            }
            else
            {
                Add("E002",
                    "Ожидалось имя константы (идентификатор).",
                    CurrentPos(tokens, pos));
            }

            // Если конец — неполное
            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление константы.", CurrentPos(tokens, pos));
                return errs;
            }

            // Двоеточие
            if (tokens[pos].Type == TokenType.COLON)
                pos++;
            else
            {
                Add("E003",
                    "Ожидался символ ':' после имени константы.",
                    tokens[pos].StartIndex);
                pos++;
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление константы.", CurrentPos(tokens, pos));
                return errs;
            }

            // Тип
            if (tokens[pos].Type == TokenType.TYPE)
            {
                pos++;
            }
            else
            {
                Add("E004",
                    "Ожидался тип (f32 или f64).",
                    tokens[pos].StartIndex);
                pos++;
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление константы.", CurrentPos(tokens, pos));
                return errs;
            }

            // Равно
            if (tokens[pos].Type == TokenType.EQUAL)
                pos++;
            else
            {
                Add("E005",
                    "Ожидался символ '=' после типа.",
                    tokens[pos].StartIndex);
                pos++;
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление константы.", CurrentPos(tokens, pos));
                return errs;
            }

            // Число
            if (tokens[pos].Type == TokenType.NUMBER)
            {
                if (CyrillicRegex.IsMatch(tokens[pos].Value)
                    || !FloatRegex.IsMatch(tokens[pos].Value))
                {
                    Add("E013",
                        $"Неверный формат числового литерала: '{tokens[pos].Value}'.",
                        tokens[pos].StartIndex);
                }
                pos++;
            }
            else
            {
                Add("E006",
                    "Ожидалось числовое значение для инициализации.",
                    tokens[pos].StartIndex);
                pos++;
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление константы.", CurrentPos(tokens, pos));
                return errs;
            }

            // Точка-запятая
            if (tokens[pos].Type == TokenType.SEMICOLON)
                pos++;
            else
            {
                Add("E007",
                    "Ожидался символ ';' в конце объявления.",
                    tokens[pos].StartIndex);
                // не делаем pos++ — дальше всё равно пропустим в цикле
            }

            // Перепрыгнуть на следующий CONST
            while (pos < tokens.Count && tokens[pos].Type != TokenType.CONST)
                pos++;

            return errs;
        }

        // ── ВСПОМОГАТЕЛИ ──
        private static ErrorDetail MakeError(string code, string msg, string input, int idx) =>
            new ErrorDetail
            {
                ErrorCode = code,
                ErrorMessage = msg,
                Position = GetLineAndColumn(input, idx)
            };

        private static int CurrentPos(List<Token> tokens, int pos) =>
            pos < tokens.Count
                ? tokens[pos].StartIndex
                : tokens.Count > 0
                    ? tokens[^1].StartIndex + tokens[^1].Value.Length
                    : 0;

        private static string GetLineAndColumn(string text, int index)
        {
            int line = 1, col = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n') { line++; col = 1; }
                else { col++; }
            }
            return $"Строка {line}, Колонка {col}";
        }
    }
}
