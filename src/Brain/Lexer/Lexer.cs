using System;
using System.Collections.Generic;

namespace Brain
{
    public class Lexer
    {
        private int currentJumpID;
        private int position;
        private string code;
        private List<Token> result;

        public List<Token> Scan(string source)
        {
            currentJumpID = 0;
            position = 0;
            result = new List<Token>();
            code = source;
            while (position < code.Length)
            {
                switch ((char)readChar())
                {
                    case '+':
                        result.Add(new Token(TokenType.Plus));
                        break;
                    case '-':
                        result.Add(new Token(TokenType.Minus));
                        break;
                    case '>':
                        result.Add(new Token(TokenType.IncrementCell));
                        break;
                    case '<':
                        result.Add(new Token(TokenType.DecrementCell));
                        break;
                    case '[':
                        result.Add(new Token(TokenType.BeginLoop) { JumpID = ++currentJumpID });
                        break;
                    case ']':
                        result.Add(new Token(TokenType.EndLoop) { JumpID = currentJumpID-- });
                        break;
                    case '.':
                        result.Add(new Token(TokenType.Out));
                        break;
                    case ',':
                        result.Add(new Token(TokenType.In));
                        break;
                }
            }
            return result;
        }

        private int peekChar(int n = 0)
        {
            return position < code.Length ? code[position] : -1;
        }
        private int readChar()
        {
            return position < code.Length ? code[position++] : -1;
        }
    }
}

