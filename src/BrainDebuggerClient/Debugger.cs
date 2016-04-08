using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BrainDebuggerClient
{
    public class DebuggerClient
    {
        public string CurrentPointer { get; private set; }
        public List<string> Pointers { get; private set; }
        private TcpMessenger messenger;
        public DebuggerClient(string ip, int port)
        {
            messenger = new TcpMessenger(ip, port);
            messenger.TcpMessengerRecieved += messenger_TcpMessengerRecieved;
            Pointers = new List<string>();
        }

        private void messenger_TcpMessengerRecieved(object sender, TcpMessengerRecievedEventArgs e)
        {
            if (e.Message == null || e.Message == "") return;
            string type = e.Message.Substring(0, e.Message.IndexOf(":")).ToLower();
            string value = e.Message.Substring(e.Message.IndexOf(":") + 1);

            switch (type)
            {
                case "pointer":
                    CurrentPointer = value;
                    Pointers.Add(value);
                    break;
                case "stdout":
                    Console.Write(value);
                    break;
                case "stdin":
                    messenger.Send("stdin:" + Console.Read().ToString());
                    break;
            }
        }

        private class TcpMessenger
        {
            private StreamWriter writer;
            private StreamReader reader;
            public TcpMessenger(string ip, int port)
            {
                TcpClient client = new TcpClient(ip, port);
                writer = new StreamWriter(client.GetStream());
                reader = new StreamReader(client.GetStream());

                new Thread(() => listenForMessages()).Start();
            }

            public void Send(string data)
            {
                writer.WriteLine(data);
                writer.Flush();
            }

            private void listenForMessages()
            {
                while (true)
                {
                    string message = reader.ReadLine();
                    if (message == "PING")
                        Send("PONG");
                    else
                        OnTcpMessengerRecieved(new TcpMessengerRecievedEventArgs { Message = message });
                }
            }

            public event EventHandler<TcpMessengerRecievedEventArgs> TcpMessengerRecieved;
            protected virtual void OnTcpMessengerRecieved(TcpMessengerRecievedEventArgs e)
            {
                EventHandler<TcpMessengerRecievedEventArgs> handler = TcpMessengerRecieved;
                if (handler != null)
                    handler(this, e);
            }
        }

        private class TcpMessengerRecievedEventArgs : EventArgs
        {
            public string Message { get; set; }
        }
    }
}

