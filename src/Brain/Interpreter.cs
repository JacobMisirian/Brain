using System;
using System.Collections.Generic;
using System.Threading;

namespace Brain
{
    public class Interpreter
    {
        private byte[] ram;
        private int pointer;
        public Interpreter()
        {
            ram = new byte[0xFFF];
            pointer = 0;
        }

        public void Execute(List<Token> tokens, Debugger debugger)
        {
            for (int position = 0; position < tokens.Count; position++)
            {
                while (debugger.Stop) Thread.Sleep(15);

                debugger.WritePointer(pointer);
                int jid;
                switch (tokens[position].TokenType)
                {
                    case TokenType.BeginLoop:
                        jid = tokens[position].JumpID;
                        if (ram[pointer] == 0)
                            while (tokens[++position].JumpID != jid)
                                ;
                        break;
                    case TokenType.EndLoop:
                        jid = tokens[position].JumpID;
                        if (ram[pointer] != 0)
                            while (tokens[--position].JumpID != jid)
                                ;
                        break;
                    case TokenType.Plus:
                        ram[pointer]++;
                        break;
                    case TokenType.Minus:
                        ram[pointer]--;
                        break;
                    case TokenType.IncrementCell:
                        pointer++;
                        break;
                    case TokenType.DecrementCell:
                        pointer--;
                        break;
                    case TokenType.In:
                        ram[pointer] = (byte)debugger.Read();
                        break;
                    case TokenType.Out:
                        debugger.Write(Convert.ToChar(ram[pointer]).ToString());
                        break;
                }
            }
        }
    }
}