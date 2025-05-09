﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TFLandCOMP.Models
{
    public class Parser
    {
        private readonly HashSet<string> declaredVariables = new HashSet<string>();

        private static readonly Regex FloatRegex = new Regex(@"^[+-]?\d+\.\d+$", RegexOptions.Compiled);
        private static readonly Regex CyrillicRegex = new Regex(@"\p{IsCyrillic}", RegexOptions.Compiled);

        public List<ErrorDetail> ParseDeclarations(List<Token> tokens, string input)
        {
            var errors = new List<ErrorDetail>();
            int pos = 0;
            while (pos < tokens.Count)
            {
                // ---- Проверяем CONST ----
                if (tokens[pos].Type != TokenType.CONST)
                {
                    errors.Add(MakeError("E001", $"Ожидалось ключевое слово 'const', но найдено '{tokens[pos].Value}'.", input, tokens[pos].StartIndex));
                }

                errors.AddRange(ParseSingleDeclaration(tokens, ref pos, input));
            }
            return errors;
        }

        private List<ErrorDetail> ParseSingleDeclaration(List<Token> tokens, ref int pos, string input)
        {
            var errs = new List<ErrorDetail>();
            void Add(string c, string m, int i) => errs.Add(MakeError(c, m, input, i));

            int startPos = pos; // чтобы вернуться, если нужно

            // ---------- CONST ----------
            if (pos < tokens.Count && tokens[pos].Type == TokenType.CONST)
            {
                if (tokens[pos].MissingSpace) Add("E010", "Отсутствует пробел между 'const' и именем переменной.", tokens[pos].StartIndex);
                if (tokens[pos].InvalidKeyword) Add("E011", $"Некорректное написание 'const': '{tokens[pos].InvalidCharacters}'.", tokens[pos].StartIndex);
                pos++;
            }

            // Если после возможного CONST строка закончилась — неполное объявление
            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление строки в рамках задания.", CurrentPos(tokens, pos));
                return errs;
            }

            // ---------- IDENTIFIER ----------
            if (tokens[pos].Type == TokenType.IDENTIFIER || tokens[pos].Type == TokenType.INVALID_IDENTIFIER)
            {
                if (tokens[pos].Type == TokenType.INVALID_IDENTIFIER)
                    Add("E014", $"Недопустимые символы в имени: '{tokens[pos].Value}'.", tokens[pos].StartIndex);
                else if (!declaredVariables.Add(tokens[pos].Value))
                    Add("E008", "Такая переменная уже существует.", tokens[pos].StartIndex);
                pos++;
            }
            else
            {
                Add("E002", "Ожидалось имя константы (идентификатор).", CurrentPos(tokens, pos));
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление строки в рамках задания.", CurrentPos(tokens, pos));
                return errs;
            }

            // ---------- ':' ----------
            if (tokens[pos].Type == TokenType.COLON)
                pos++;
            else
                Add("E003", "Ожидался символ ':' после имени константы.", tokens[pos].StartIndex);

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление строки в рамках задания.", CurrentPos(tokens, pos));
                return errs;
            }

            // ---------- TYPE ----------
            if (tokens[pos].Type == TokenType.TYPE)
            {
                if (tokens[pos].InvalidKeyword || tokens[pos].MissingSpace)
                    Add("E004", $"Некорректный тип: '{tokens[pos].Value}'.", tokens[pos].StartIndex);
                pos++;
            }
            else
            {
                Add("E004", "Ожидался тип (f32 или f64).", tokens[pos].StartIndex);
                pos++; // пропускаем неверный токен, чтобы избежать лавины ошибок
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление строки в рамках задания.", CurrentPos(tokens, pos));
                return errs;
            }

            // ---------- '=' ----------
            if (tokens[pos].Type == TokenType.EQUAL)
                pos++;
            else
            {
                Add("E005", "Ожидался символ '=' после типа.", tokens[pos].StartIndex);
                pos++;
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление строки в рамках задания.", CurrentPos(tokens, pos));
                return errs;
            }

            // ---------- NUMBER ----------
            if (tokens[pos].Type == TokenType.NUMBER)
            {
                if (CyrillicRegex.IsMatch(tokens[pos].Value) || !FloatRegex.IsMatch(tokens[pos].Value))
                    Add("E013", $"Неверный формат числового литерала: '{tokens[pos].Value}'.", tokens[pos].StartIndex);
                pos++;
            }
            else
            {
                Add("E006", "Ожидалось числовое значение для инициализации.", tokens[pos].StartIndex);
                pos++;
            }

            if (pos >= tokens.Count)
            {
                Add("E015", "Неполное объявление строки в рамках задания.", CurrentPos(tokens, pos));
                return errs;
            }

            // ---------- ';' ----------
            if (tokens[pos].Type == TokenType.SEMICOLON)
                pos++;
            else
                Add("E007", "Ожидался символ ';' в конце объявления.", tokens[pos].StartIndex);

            // пропустить до следующего const
            while (pos < tokens.Count && tokens[pos].Type != TokenType.CONST)
                pos++;

            return errs;
        }

        // ---------- helpers ----------
        private static ErrorDetail MakeError(string code, string msg, string input, int idx) =>
            new ErrorDetail { ErrorCode = code, ErrorMessage = msg, Position = GetLineAndColumn(input, idx) };

        private static int CurrentPos(List<Token> tokens, int pos) =>
            pos < tokens.Count ? tokens[pos].StartIndex : (tokens.Count > 0 ? tokens[^1].StartIndex + tokens[^1].Value.Length : 0);

        private static string GetLineAndColumn(string text, int index)
        {
            int line = 1, col = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n') { line++; col = 1; }
                else col++;
            }
            return $"Строка {line}, Колонка {col}";
        }
    }
}
