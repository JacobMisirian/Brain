using System;
using System.IO;
using System.Net;

namespace Brain
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Debugger debugger = new Debugger(IPAddress.Parse("127.0.0.1"), 1337, args[0] == "-d");
            new Interpreter().Execute(new Lexer().Scan(File.ReadAllText("test.br")), debugger);
        }
    }
}
