using System.Collections.Generic;

using TFLandCOMP.Models;

namespace TFLandCOMP.Services
{
    public class QuadrupleGenerator
    {
        private List<Token> tokens;
        private int pos;
        private int tempCounter;
        public List<Quadruple> Quads { get; private set; }

        public List<Quadruple> Generate(List<Token> tokens)
        {
            this.tokens = tokens;
            this.pos = 0;
            this.tempCounter = 0;
            this.Quads = new List<Quadruple>();

            string result = ParseE();
            return Quads;
        }

        private string ParseE()
        {
            string t = ParseT();
            while (Match(TokenType.PLUS) || Match(TokenType.MINUS))
            {
                string op = Current().Value;
                Advance();
                string t2 = ParseT();
                string temp = NewTemp();
                Quads.Add(new Quadruple { Operation = op, Arg1 = t, Arg2 = t2, Result = temp });
                t = temp;
            }
            return t;
        }

        private string ParseT()
        {
            string o = ParseO();
            while (Match(TokenType.STAR) || Match(TokenType.SLASH))
            {
                string op = Current().Value;
                Advance();
                string o2 = ParseO();
                string temp = NewTemp();
                Quads.Add(new Quadruple { Operation = op, Arg1 = o, Arg2 = o2, Result = temp });
                o = temp;
            }
            return o;
        }

        private string ParseO()
        {
            if (Match(TokenType.IDENTIFIER))
            {
                string val = Current().Value;
                Advance();
                return val;
            }
            else if (Match(TokenType.LPAREN))
            {
                Advance();
                string inner = ParseE();
                if (Match(TokenType.RPAREN))
                    Advance();
                return inner;
            }
            else
            {
                string unknown = Current().Value;
                Advance();
                return unknown;
            }
        }

        private Token Current() => pos < tokens.Count ? tokens[pos] : null;
        private bool Match(TokenType type) => pos < tokens.Count && tokens[pos].Type == type;
        private void Advance() => pos++;
        private string NewTemp() => $"t{tempCounter++}";
    }
}
