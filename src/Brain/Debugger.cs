using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Brain
{
    public class Debugger
    {
        public bool Stop { get; set; }
        private TcpDebugger tcpDebugger;
        private bool waitForConnection;
        public Debugger(IPAddress ip, int port, bool waitForConnection)
        {
            tcpDebugger = new TcpDebugger(ip, port);
            tcpDebugger.DebuggerClientDisconnected += tcpDebugger_OnDebuggerClientDisconnected;
            tcpDebugger.DebuggerClientConnected += tcpDebugger_OnDebuggerClientConnected;
            tcpDebugger.DebuggerMessageRecieved += tcpDebugger_OnDebuggerMessageRecieved;
            this.waitForConnection = waitForConnection;
            Stop = waitForConnection;
        }

        public void Write(string data)
        {
            Console.Write(data);
            foreach (DebuggerClient client in tcpDebugger.Clients)
                client.Write("stdout:" + data);
        }

        private bool requestingStdin = false;
        private char stdin = '\0';
        public int Read()
        {
            if (tcpDebugger.Clients.Count == 0)
                return (int)Console.Read();
            requestingStdin = true;
            foreach (DebuggerClient client in tcpDebugger.Clients)
                client.Write("stdin::");
            while (requestingStdin) Thread.Sleep(15);
            return (int)stdin;
        }

        public void WritePointer(int pointer)
        {
            foreach (DebuggerClient client in tcpDebugger.Clients)
                client.Write("pointer:" + pointer.ToString());
        }

        private void tcpDebugger_OnDebuggerClientDisconnected(object sender, DebuggerClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Debugger client disconnected!");
            e.DebuggerClient.ListenerThread.Abort();
            e.DebuggerClient.PingThread.Abort();
            tcpDebugger.Clients.Remove(e.DebuggerClient);
        }
        private void tcpDebugger_OnDebuggerClientConnected(object sender, DebuggerClientConnectedEventArgs e)
        {
            Console.WriteLine("Debugger client connected!");
            if (waitForConnection)
                Stop = false;
        }
        private void tcpDebugger_OnDebuggerMessageRecieved(object sender, DebuggerMessageRecievedEventArgs e)
        {
            if (e.Message == null || e.Message == "") return;
            string type = e.Message.Substring(0, e.Message.IndexOf(":")).ToLower();
            string value = e.Message.Substring(e.Message.IndexOf(":") + 1);

            switch (type)
            {
                case "stdin":
                    requestingStdin = false;
                    stdin = Convert.ToChar(Convert.ToInt32(value));
                    break;
                case "stop":
                    Stop = true;
                    break;
                case "start":
                    Stop = false;
                    break;
            }
        }

        private class TcpDebugger
        {
            public List<DebuggerClient> Clients { get; private set; }
            private TcpListener listener;
            public TcpDebugger(IPAddress ip, int port)
            {
                Clients = new List<DebuggerClient>();
                listener = new TcpListener(ip, port);
                listener.Start();
                new Thread(() => listenForConnections()).Start();
            }

            private void listenForConnections()
            {
                while (true)
                {
                    DebuggerClient client = new DebuggerClient(listener.AcceptTcpClient());
                    Clients.Add(client);
                    client.ListenerThread = new Thread(() => listenForMessages(client));
                    client.ListenerThread.Start();
                    client.PingThread = new Thread(() => sendPing(client));
                    client.PingThread.Start();
                    OnDebuggerClientConnected(new DebuggerClientConnectedEventArgs { DebuggerClient = client });
                }
            }

            private void listenForMessages(DebuggerClient client)
            {
                try
                {
                    while (true)
                    {
                        string message = client.Reader.ReadLine();
                        if (message == "PONG")
                            client.Ping = 0;
                        else
                            OnDebuggerMessageRecieved(new DebuggerMessageRecievedEventArgs { DebuggerClient = client, Message = message });
                        Thread.Sleep(20);
                    }
                }
                catch (IOException)
                {
                    OnDebuggerClientDisconnected(new DebuggerClientDisconnectedEventArgs { DebuggerClient = client });
                }
            }
            private void sendPing(DebuggerClient client)
            {
                try
                {
                    while (client.Ping <= 10000)
                    {
                        client.Write("PING");
                        client.Ping += 1000;
                        Thread.Sleep(1000);
                    }
                }
                catch (IOException)
                {
                    OnDebuggerClientDisconnected(new DebuggerClientDisconnectedEventArgs { DebuggerClient = client });
                }
                OnDebuggerClientDisconnected(new DebuggerClientDisconnectedEventArgs { DebuggerClient = client });
            }

            public event EventHandler<DebuggerClientConnectedEventArgs> DebuggerClientConnected;
            public event EventHandler<DebuggerClientDisconnectedEventArgs> DebuggerClientDisconnected;
            public event EventHandler<DebuggerMessageRecievedEventArgs> DebuggerMessageRecieved;
            protected virtual void OnDebuggerClientConnected(DebuggerClientConnectedEventArgs e)
            {
                EventHandler<DebuggerClientConnectedEventArgs> handler = DebuggerClientConnected;
                if (handler != null)
                    handler(this, e);
            }
            protected virtual void OnDebuggerClientDisconnected(DebuggerClientDisconnectedEventArgs e)
            {
                EventHandler<DebuggerClientDisconnectedEventArgs> handler = DebuggerClientDisconnected;
                if (handler != null)
                    handler(this, e);
            }
            protected virtual void OnDebuggerMessageRecieved(DebuggerMessageRecievedEventArgs e)
            {
                EventHandler<DebuggerMessageRecievedEventArgs> handler = DebuggerMessageRecieved;
                if (handler != null)
                    handler(this, e);
            }
        }

        private class DebuggerClient
        {
            public TcpClient TcpClient { get; private set; }
            public StreamWriter Writer { get; private set; }
            public StreamReader Reader { get; private set; }
            public int Ping { get; set; }
            public Thread ListenerThread { get; set; }
            public Thread PingThread { get; set; }

            public DebuggerClient(TcpClient client)
            {
                TcpClient = client;
                Writer = new StreamWriter(client.GetStream());
                Reader = new StreamReader(client.GetStream());
                Ping = 0;
            }

            public void Write(string data)
            {
                Writer.WriteLine(data);
                Writer.Flush();
            }
        }
        private class DebuggerMessageRecievedEventArgs : EventArgs
        {
            public DebuggerClient DebuggerClient { get; set; }
            public string Message { get; set; }
        }
        private class DebuggerClientConnectedEventArgs : EventArgs
        {
            public DebuggerClient DebuggerClient { get; set; }
        }
        private class DebuggerClientDisconnectedEventArgs : EventArgs
        {
            public DebuggerClient DebuggerClient { get; set; }
        }
    }
}