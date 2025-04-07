using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

namespace TFLandCOMP.ViewModels
{
    public partial class MainViewViewModel : ObservableObject
    {
        public ObservableCollection<ErrorDetail> Errors { get; set; } = new ObservableCollection<ErrorDetail>();

        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public IRelayCommand RunScanCommand { get; }

        private HashSet<string> declaredVariables = new HashSet<string>();

        public MainViewViewModel()
        {
            RunScanCommand = new RelayCommand(RunScan);
        }

        // Запуск анализа: лексер и разбор всех объявлений
        private void RunScan()
        {
            Errors.Clear();
            declaredVariables.Clear();
            var tokens = Lex(InputText);
            var parseErrors = ParseDeclarations(tokens, InputText);
            if (parseErrors.Count == 0)
            {
                Errors.Add(new ErrorDetail
                {
                    ErrorCode = "OK",
                    ErrorMessage = "Ошибок не обнаружено",
                    Position = ""
                });
            }
            else
            {
                foreach (var error in parseErrors)
                {
                    Errors.Add(error);
                }
            }
        }

        // Лексер – разбивает входной текст на токены
        private List<Token> Lex(string input)
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
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_'))
                    {
                        i++;
                    }
                    string word = input.Substring(start, i - start);
                    // Если слово начинается с "const" и содержит лишние символы, значит отсутствует пробел
                    if (word.StartsWith("const") && word.Length > "const".Length)
                    {
                        tokens.Add(new Token(TokenType.CONST, "const", start) { MissingSpace = true });
                        string remainder = word.Substring("const".Length);
                        tokens.Add(new Token(TokenType.IDENTIFIER, remainder, start + "const".Length));
                    }
                    else
                    {
                        if (word == "const")
                        {
                            tokens.Add(new Token(TokenType.CONST, word, start));
                        }
                        else if (word == "f32" || word == "f64")
                        {
                            tokens.Add(new Token(TokenType.TYPE, word, start));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.IDENTIFIER, word, start));
                        }
                    }
                    continue;
                }
                if (char.IsDigit(c))
                {
                    int start = i;
                    // Новая логика: включаем все символы до разделителя
                    while (i < input.Length && !char.IsWhiteSpace(input[i]) &&
                           input[i] != ':' && input[i] != '=' && input[i] != ';')
                    {
                        i++;
                    }
                    string number = input.Substring(start, i - start);
                    tokens.Add(new Token(TokenType.NUMBER, number, start));
                    continue;
                }
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

        private List<ErrorDetail> ParseDeclarations(List<Token> tokens, string input)
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
                    while (pos < tokens.Count && tokens[pos].Type != TokenType.SEMICOLON)
                    {
                        pos++;
                    }
                    if (pos < tokens.Count && tokens[pos].Type == TokenType.SEMICOLON)
                        pos++;
                    continue;
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
            int maxErrors = 3;

            void AddError(string code, string message, int tokenPos)
            {
                if (errors.Count < maxErrors)
                {
                    errors.Add(new ErrorDetail
                    {
                        ErrorCode = code,
                        ErrorMessage = message,
                        Position = GetLineAndColumn(input, tokenPos)
                    });
                }
            }

            Token constToken = tokens[pos];
            if (constToken.MissingSpace)
            {
                AddError("E010", "Отсутствует пробел после ключевого слова 'const'.", constToken.StartIndex);
            }
            pos++; 

           // Ожидаем идентификатор
            string varName = null;
            if (pos < tokens.Count && tokens[pos].Type == TokenType.IDENTIFIER)
            {
                varName = tokens[pos].Value;
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

            if (pos < tokens.Count && tokens[pos].Type == TokenType.TYPE)
            {
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

            if (pos < tokens.Count && tokens[pos].Type == TokenType.NUMBER)
            {
                string numberStr = tokens[pos].Value;
                if (!Regex.IsMatch(numberStr, @"^\d+(\.\d+)?$"))
                {
                    AddError("E009", "Неверный формат числового литерала.", tokens[pos].StartIndex);
                }
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

            if (pos < tokens.Count && tokens[pos].Type == TokenType.SEMICOLON)
            {
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E007", "Ожидался символ ';' в конце объявления.", tokens[pos].StartIndex);
                else
                    AddError("E007", "Ожидался символ ';' в конце объявления, но не найден.", input.Length);
                while (pos < tokens.Count && tokens[pos].Type != TokenType.CONST)
                    pos++;
            }

            return errors;
        }

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

        private class Token
        {
            public TokenType Type { get; }
            public string Value { get; }
            public int StartIndex { get; }
            public bool MissingSpace { get; set; } = false;
            public Token(TokenType type, string value, int startIndex)
            {
                Type = type;
                Value = value;
                StartIndex = startIndex;
            }
        }

        private enum TokenType
        {
            CONST,
            IDENTIFIER,
            COLON,
            TYPE,
            EQUAL,
            NUMBER,
            SEMICOLON,
            UNKNOWN
        }
    }

    public class ErrorDetail
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Position { get; set; }
    }
}
