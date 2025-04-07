using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;

namespace TFLandCOMP.ViewModels
{
    public partial class MainViewViewModel : ObservableObject
    {
        // Коллекция для вывода ошибок с детализацией
        public ObservableCollection<ErrorDetail> Errors { get; set; } = new ObservableCollection<ErrorDetail>();

        // Текст, вводимый пользователем (привязан к редактору)
        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        // Команда для запуска анализа текста
        public IRelayCommand RunScanCommand { get; }

        public MainViewViewModel()
        {
            RunScanCommand = new RelayCommand(RunScan);
        }

        // Запуск анализа: лексический разбор и разбор всех объявлений
        private void RunScan()
        {
            Errors.Clear();
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
                    continue;
                }
                if (char.IsDigit(c))
                {
                    int start = i;
                    bool hasDot = false;
                    while (i < input.Length && (char.IsDigit(input[i]) || (!hasDot && input[i] == '.')))
                    {
                        if (input[i] == '.')
                            hasDot = true;
                        i++;
                    }
                    string number = input.Substring(start, i - start);
                    tokens.Add(new Token(TokenType.NUMBER, number, start));
                    continue;
                }
                // Обработка одиночных символов
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

        // Обрабатывает все объявления во входном тексте
        private List<ErrorDetail> ParseDeclarations(List<Token> tokens, string input)
        {
            List<ErrorDetail> errors = new List<ErrorDetail>();
            int pos = 0;
            while (pos < tokens.Count)
            {
                var declErrors = ParseSingleDeclaration(tokens, ref pos, input);
                errors.AddRange(declErrors);
            }
            return errors;
        }

        // Разбирает одно объявление, собирая до 3 ошибок
        private List<ErrorDetail> ParseSingleDeclaration(List<Token> tokens, ref int pos, string input)
        {
            List<ErrorDetail> errors = new List<ErrorDetail>();
            int maxErrors = 3;

            // Вспомогательный метод для добавления ошибки, если ещё не набрано 3 ошибки
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

            // Последовательность ожидаемых токенов: CONST, IDENTIFIER, COLON, TYPE, EQUAL, NUMBER, SEMICOLON

            // Проверка "const"
            if (pos < tokens.Count && tokens[pos].Type == TokenType.CONST)
            {
                pos++;
            }
            else
            {
                if (pos < tokens.Count)
                    AddError("E001", $"Ожидалось ключевое слово 'const', но найдено '{tokens[pos].Value}'.", tokens[pos].StartIndex);
                else
                    AddError("E001", "Ожидалось ключевое слово 'const', но не найдено.", input.Length);
                pos++;
            }

            // Проверка идентификатора
            if (pos < tokens.Count && tokens[pos].Type == TokenType.IDENTIFIER)
            {
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

            // Проверка символа ':'
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

            // Проверка типа (например, f32 или f64)
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

            // Проверка символа '='
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

            // Проверка числового литерала
            if (pos < tokens.Count && tokens[pos].Type == TokenType.NUMBER)
            {
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

            // Проверка символа ';'
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
                // Восстановление: пропускаем токены до следующего объявления (до "const") или до конца
                while (pos < tokens.Count && tokens[pos].Type != TokenType.CONST)
                {
                    pos++;
                }
            }

            return errors;
        }

        // Вспомогательный метод для вычисления строки и колонки по индексу
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

        // Вспомогательный класс для представления токена с позицией
        private class Token
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

    // Класс для хранения деталей ошибки
    public class ErrorDetail
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Position { get; set; }
    }
}
