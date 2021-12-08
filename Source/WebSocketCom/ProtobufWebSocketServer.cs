using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;


namespace WebCom
{

    public class ProtobufWebSocketServer : ISessionManager
    {
        WebSocketServer _server;

        public event SessionDelegate NewSessionConnected
        {
            add
            {
                _server.NewSessionConnected += value;
            }
            remove
            {
                _server.NewSessionConnected -= value;
            }
        }

        public event SessionDelegate SessionDisconnected
        {
            add
            {

                _server.SessionDisconnected += value;
            }
            remove
            {
                _server.SessionDisconnected -= value;
            }
        }

        public event DataReceivedDelegate DataReceived
        {
            add
            {
                _server.DataReceived += value;
            }
            remove
            {
                _server.DataReceived -= value;
            }
        }

        public event MessageReceivedDelegate MessageReceived
        {
            add
            {
                _server.MessageReceived += value;
            }
            remove
            {
                _server.MessageReceived -= value;
            }
        }

        public ProtobufWebSocketServer()
        {
            _server = new WebSocketServer();
        }

        public bool Start(int port)
        {
            try
            {
                return _server.Start(port);
            }
            catch(Exception ex)
            {
                Console.WriteLine("exception: " + ex.Message + ex.StackTrace);
                return false;
            }
        }

        public int CountConnections()
        {
            return _server.Count;
        }

        public void Close()
        {
            _server.StopAsync().Wait();
        }

        public void Send(string message, string sessionId = "")
        {
            var msgbuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            _server.Broadcast(msgbuf);
        }

        public bool Send(IMessage message, string sessionId = "")
        {
            byte[] arr = message.ToByteArray();
            return Send(arr, sessionId);
        }

        public bool Send(byte[] message, string sessionId = "")
        {
            var msgbuf = new ArraySegment<byte>(message);
            return _server.Broadcast(msgbuf);       
        }

        public void SendTo(object session, string message)
        {
            try
            {
                var connectedClient = session as WebCom.ConnectedClient;
                if (connectedClient != null)
                {
                    var msgbuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                    connectedClient.BroadcastQueue.Add(msgbuf);
                }
                else
                {
                    ((ProtobufWebSocketServer)session).Send(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception: " + ex.Message + ex.StackTrace);
            }
        }

        public void CloseConnection()
        {
            _server.StopAsync().Wait();
        }
    }
}
