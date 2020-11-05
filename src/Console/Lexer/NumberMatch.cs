﻿using UnityEngine;

namespace UnityExplorer.Console.Lexer
{
    public sealed class NumberMatch : Matcher
    {
        public override Color HighlightColor => new Color(0.58f, 0.33f, 0.33f, 1.0f);

        public override bool IsImplicitMatch(InputLexer lexer)
        {
            if (!char.IsWhiteSpace(lexer.Previous) &&
                !lexer.IsSpecialSymbol(lexer.Previous, SpecialCharacterPosition.End))
            {
                return false;
            }

            bool matchedNumber = false;

            while (!lexer.EndOfStream)
            {
                if (IsNumberOrDecimalPoint(lexer.ReadNext()))
                {
                    matchedNumber = true;
                    lexer.Commit();
                }
                else
                {
                    lexer.Rollback();
                    break;
                }
            }

            return matchedNumber;
        }

        private bool IsNumberOrDecimalPoint(char character) => char.IsNumber(character) || character == '.';
    }

}
