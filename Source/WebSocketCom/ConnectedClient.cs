using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace WebCom
{
    public class ConnectedClient
    {
        WebSocketMessageType _messageType;

        public ConnectedClient(int socketId, WebSocket socket, WebSocketMessageType messageType = WebSocketMessageType.Binary)
        {
            if(socket != null)
            {
                Console.WriteLine("client created with id: " + SocketId + " socket: " + socket.ToString());
                SocketId = socketId;
                Socket = socket;
                _messageType = messageType;
            }
            else
            {
                Console.WriteLine("websocket is null");
            }
        }

        public int SocketId { get; private set; }

        public WebSocket Socket { get; private set; }

        public BlockingCollection<ArraySegment<byte>> BroadcastQueue { get; } = new BlockingCollection<ArraySegment<byte>>();

        public CancellationTokenSource BroadcastLoopTokenSource { get; set; } = new CancellationTokenSource();

        public async Task BroadcastLoopAsync()
        {
            var cancellationToken = BroadcastLoopTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = BroadcastQueue.Take(cancellationToken);
                    Console.WriteLine($"Socket {SocketId}: Sending from queue.");
                    await Socket.SendAsync(message, _messageType, true, CancellationToken.None);
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine("catch (OperationCanceledException) in BroadcastLoopAsync() " + ex.Message + ex.StackTrace);
                    // normal upon task/token cancellation, disregard
                }
                catch (Exception ex)
                {         
                    if (ex.InnerException!=null && ex.InnerException.Message.Contains("nonexistent network connection"))
                    {
                        Console.WriteLine("Network connection closed");
                    }
                    else
                    {
                        Console.WriteLine("exception: " + ex.Message + ex.StackTrace + "InnerException: " + ex.InnerException.Message + ex.InnerException.StackTrace);
                    }
                }
            }
        }

    }
}
