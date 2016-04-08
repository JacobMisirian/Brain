using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BrainDebuggerClient
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            new DebuggerClient(args[0], Convert.ToInt32(args[1]));
            Console.Read();
        }
    }
}
