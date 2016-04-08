using System;

namespace Brain
{
    public class Token
    {
        public int JumpID { get; set; }
        public TokenType TokenType { get; set; }
        public Token(TokenType tokenType)
        {
            JumpID = 0;
            TokenType = tokenType;
        }
    }

    public enum TokenType
    {
        Plus,
        Minus,
        IncrementCell,
        DecrementCell,
        BeginLoop,
        EndLoop,
        Out,
        In
    }
}

