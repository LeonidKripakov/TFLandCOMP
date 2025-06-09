using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TFLandCOMP.Services
{
    public static class Patterns
    {
        // 1. Слова, не заканчивающиеся на 't' или 'T'
        public static readonly Regex WordsNotEndingWithT =
            new Regex(@"\b[a-zA-Z]+(?<![tT])\b", RegexOptions.Compiled);

        // 2. Юзернейм: цифры, строчные буквы, '-' и '_', длина 5–20
        public static readonly Regex Username =
            new Regex(@"\b[a-z0-9_-]{5,20}\b", RegexOptions.Compiled);

        // 3. Долгота в градусах: от –180 до +180, с необязательной дробной частью
        public static readonly Regex Longitude = new Regex(
    @"(?<![A-Za-z\d.])[+-]?(?:(?:1[0-7]\d|[1-9]?\d)(?:\.\d+)?|180(?:\.0+)?)(?![A-Za-z\d.])",
    RegexOptions.Compiled);
    }
}
