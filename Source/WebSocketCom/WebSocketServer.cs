using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace WebCom
{
    public class WebSocketServer
    {


        private static HttpListener Listener;

        private static CancellationTokenSource SocketLoopTokenSource;
        private static CancellationTokenSource ListenerLoopTokenSource;

        private static int SocketCounter = 0;

        private static bool ServerIsRunning = true;

        private const int CLOSE_SOCKET_TIMEOUT_MS = 2500;
        private const int MESSAGE_MAX_CHUNK_SIZE = 4096;

        private SessionDelegate _sessionNewConnectionHandlers;

        public int Count { get { return SocketCounter; } }

        public event SessionDelegate NewSessionConnected
        {
            add
            {
                if (_sessionNewConnectionHandlers == null)
                {
                    _sessionNewConnectionHandlers = value;
                }
                else
                {
                    lock (_sessionNewConnectionHandlers)
                    {
                        _sessionNewConnectionHandlers += value;
                    }
                }
            }
            remove
            {
                lock (_sessionNewConnectionHandlers)
                {
                    _sessionNewConnectionHandlers -= value;
                }
            }
        }

        private SessionDelegate _sessionDisconnectHandlers;
        public event SessionDelegate SessionDisconnected
        {
            add
            {
                if (_sessionDisconnectHandlers == null)
                {
                    _sessionDisconnectHandlers = value;
                }
                else
                {
                    lock (_sessionDisconnectHandlers)
                    {
                        _sessionDisconnectHandlers += value;
                    }
                }
            }
            remove
            {
                lock (_sessionDisconnectHandlers)
                {
                    _sessionDisconnectHandlers -= value;
                }
            }
        }

        private DataReceivedDelegate _dataReceivedHandlers;
        public event DataReceivedDelegate DataReceived
        {
            add
            {
                if (_dataReceivedHandlers == null)
                {
                    _dataReceivedHandlers = value;
                }
                else
                {
                    lock (_dataReceivedHandlers)
                    {
                        _dataReceivedHandlers += value;
                    }

                }
            }
            remove
            {
                lock (_dataReceivedHandlers)
                {
                    _dataReceivedHandlers -= value;
                }
            }
        }

        private MessageReceivedDelegate _messageReceivedHandlers;
        public event MessageReceivedDelegate MessageReceived
        {
            add
            {
                if (_messageReceivedHandlers == null)
                {
                    _messageReceivedHandlers = value;
                }
                else
                {
                    lock (_messageReceivedHandlers)
                    {
                        _messageReceivedHandlers += value;
                    }
                }
            }
            remove
            {
                lock (_messageReceivedHandlers)
                {
                    _messageReceivedHandlers -= value;
                }
            }
        }


        // The key is a socket id
        private ConcurrentDictionary<int, ConnectedClient> Clients;


        public WebSocketServer()
        {
            Clients = new ConcurrentDictionary<int, ConnectedClient>();
        }

        public bool Start(int port)
        {
            try
            {
                string uriPrefix = "http://+:" + port.ToString() + "/";
                SocketLoopTokenSource = new CancellationTokenSource();
                ListenerLoopTokenSource = new CancellationTokenSource();
                Listener = new HttpListener();
                Listener.Prefixes.Add(uriPrefix);
                Listener.Start();
                if (Listener.IsListening)
                {
                    Console.WriteLine($"Server listening: {uriPrefix}");
                    // listen on a separate thread so that Listener.Stop can interrupt GetContextAsync
                    Task.Run(() => ListenerProcessingLoopAsync().ConfigureAwait(false));
                }
                else
                {
                    Console.WriteLine("Server failed to start.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception encountered while starting websocket server : " + ex.Message + ex.StackTrace);
                return false;
            }
            return true;
        }

        public async Task StopAsync()
        {
            if (Listener?.IsListening ?? false && ServerIsRunning)
            {
                Console.WriteLine("\nServer is stopping.");

                ServerIsRunning = false;            // prevent new connections during shutdown
                await CloseAllSocketsAsync();            // also cancels processing loop tokens (abort ReceiveAsync)
                ListenerLoopTokenSource.Cancel();   // safe to stop now that sockets are closed
                Listener.Stop();
                Listener.Close();
            }
        }

        public bool Broadcast(ArraySegment<byte> message)
        {
            try
            {
                if (Clients.IsEmpty)
                {
                    Console.WriteLine("no connected clients found");
                }
                else
                {
                    foreach (var kvp in Clients)
                    {
                        kvp.Value.BroadcastQueue.Add(message);
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine("exception in Broadcast: " + ex.Message + ex.StackTrace);
                return false;
            }

        }

        private async Task ListenerProcessingLoopAsync()
        {
            var cancellationToken = ListenerLoopTokenSource.Token;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context = await Listener.GetContextAsync();
                    if (ServerIsRunning)
                    {
                        if (context.Request.IsWebSocketRequest)
                        {
                            // HTTP is only the initial connection; upgrade to a client-specific websocket
                            HttpListenerWebSocketContext wsContext = null;
                            try
                            {
                                wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                                int socketId = Interlocked.Increment(ref SocketCounter);
                                var client = new ConnectedClient(socketId, wsContext.WebSocket);
                                bool added = Clients.TryAdd(socketId, client);
                                Console.WriteLine("added to clients:  " + added.ToString());
                                if (_sessionNewConnectionHandlers != null)
                                {
                                    try
                                    {
                                        lock (_sessionNewConnectionHandlers)
                                        {
                                            _sessionNewConnectionHandlers(client, socketId.ToString()); //???
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("no subscribers to sessionNewConnectionHandlers");
                                    }

                                }
                                
                                Console.WriteLine($"Socket {socketId}: New connection.");
                                _ = Task.Run(() => SocketProcessingLoopAsync(client).ConfigureAwait(false));
                            }
                            catch (Exception ex)
                            {
                                // server error if upgrade from HTTP to WebSocket fails
                                Console.WriteLine("WebSocket upgrade failed. exception: " + ex.Message + ex.StackTrace);
                                context.Response.StatusCode = 500;
                                context.Response.StatusDescription = "WebSocket upgrade failed";
                                context.Response.Close();
                                return;
                            }
                        }
                        else
                        {
                            Console.WriteLine("!context.Request.IsWebSocketRequest, returning 400 respone");
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                    else
                    {
                        Console.WriteLine("returning 409 response");
                        // HTTP 409 Conflict (with server's current state)
                        context.Response.StatusCode = 409;
                        context.Response.StatusDescription = "Server is shutting down";
                        context.Response.Close();
                        return;
                    }
                }
            }
            catch (HttpListenerException ex) when (ServerIsRunning)
            {
                Console.WriteLine("Exception: " + ex.Message + ex.StackTrace);
            }
        }

        private async Task SocketProcessingLoopAsync(ConnectedClient client)
        {
            _ = Task.Run(() => client.BroadcastLoopAsync().ConfigureAwait(false));

            var socket = client.Socket;
            var loopToken = SocketLoopTokenSource.Token;
            var broadcastTokenSource = client.BroadcastLoopTokenSource; // store a copy for use in finally block
            try
            {
                

                while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted && !loopToken.IsCancellationRequested)
                {
                    // if the token is cancelled while ReceiveAsync is blocking, the socket state changes to aborted and it can't be used
                    if (!loopToken.IsCancellationRequested)
                    {
                        if (client.Socket.State == WebSocketState.Open)
                        {
                            WebSocketReceiveResult receiveResult;
                            byte[] data;

                            using (var ms = new System.IO.MemoryStream())
                            {
                                do //get message chunks
                                {
                                    var buffer = WebSocket.CreateClientBuffer(MESSAGE_MAX_CHUNK_SIZE, 16);
                                    receiveResult = await client.Socket.ReceiveAsync(buffer, loopToken);
                                    ms.Write(buffer.Array, buffer.Offset, receiveResult.Count);
                                }
                                while (!receiveResult.EndOfMessage);

                                if (ms.Length > 0) //convert to byte array
                                {
                                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                                    ms.Position = 0;
                                    data = ms.ToArray();
                                }
                                else
                                {
                                    Console.WriteLine($"Failed receive socket {client.SocketId} result ({receiveResult.MessageType} frame, {receiveResult.Count} bytes).");
                                    continue;
                                }

                                Console.WriteLine($"Socket {client.SocketId}: Received {receiveResult.MessageType} frame ({receiveResult.Count} bytes).");

                                switch (receiveResult.MessageType)
                                {
                                    case WebSocketMessageType.Binary: //binary bytes, such as encoded protobuf message

                                        if (_dataReceivedHandlers != null)
                                        {
                                            try
                                            {
                                                lock (_dataReceivedHandlers)
                                                {
                                                    _dataReceivedHandlers(client, data);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine($"Exception while sending a received message to data handlers: {ex.Message} {ex.StackTrace}");
                                            }
                                        }
                                        break;
                                    case WebSocketMessageType.Text: 
                                        if (_messageReceivedHandlers != null)
                                        {
                                            try
                                            {
                                                lock (_messageReceivedHandlers)
                                                {
                                                    _messageReceivedHandlers(client, Encoding.UTF8.GetString(data));
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                Console.WriteLine("no subscribers to messageReceivedHandlers");
                                            }
                                        }
                                        break;
                                    case WebSocketMessageType.Close: //request to close connection
                                        if (client.Socket.State == WebSocketState.CloseReceived)
                                        {
                                            Console.WriteLine($"Socket {client.SocketId}: Acknowledging Close frame received from client");
                                            broadcastTokenSource.Cancel();
                                            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                                            // the socket state changes to closed at this point
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }

            }
            catch (OperationCanceledException)
            {
                // normal upon task/token cancellation, disregard
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception with socket " + client.SocketId + " : " + ex.Message + ex.StackTrace + "innerexception: " + ex.InnerException.Message + ex.InnerException.StackTrace);
            }
            finally
            {
                broadcastTokenSource.Cancel();

                Console.WriteLine($"Socket {client.SocketId}: Ended processing loop in state {socket.State}");

                // don't leave the socket in any potentially connected state
                if (client.Socket.State != WebSocketState.Closed)
                    client.Socket.Abort();
                //add sessionDisconnectHandlers here
                if (_sessionDisconnectHandlers != null)
                {
                    try
                    {
                        lock (_sessionDisconnectHandlers)
                        {
                            _sessionDisconnectHandlers(client, client.SocketId.ToString());
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("no subscribers to sessionDisconnectHandlers");
                    }

                }


                // by this point the socket is closed or aborted, the ConnectedClient object is useless
                if (Clients.TryRemove(client.SocketId, out _))
                    socket.Dispose();
            }
        }

        private async Task CloseAllSocketsAsync()
        {
            // We can't dispose the sockets until the processing loops are terminated,
            // but terminating the loops will abort the sockets, preventing graceful closing.
            var disposeQueue = new List<WebSocket>(Clients.Count);

            foreach (var client in Clients)
            {
                var clientValue = client.Value;
                Console.WriteLine($"Closing Socket {clientValue.SocketId}");

                Console.WriteLine("... ending broadcast loop");
                clientValue.BroadcastLoopTokenSource.Cancel();

                if (clientValue.Socket.State != WebSocketState.Open)
                {
                    Console.WriteLine($"... socket not open, state = {clientValue.Socket.State}");
                }
                else
                {
                    var timeout = new CancellationTokenSource(CLOSE_SOCKET_TIMEOUT_MS);
                    try
                    {
                        Console.WriteLine("... starting close handshake");
                        await clientValue.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // normal upon task/token cancellation, disregard
                    }
                }

                if (Clients.TryRemove(clientValue.SocketId, out _))
                {
                    // only safe to Dispose once, so only add it if this loop can't process it again
                    disposeQueue.Add(clientValue.Socket);
                }

                Console.WriteLine("... done");
            }

            // now that they're all closed, terminate the blocking ReceiveAsync calls in the SocketProcessingLoop threads
            SocketLoopTokenSource.Cancel();

            // dispose all resources
            foreach (var socket in disposeQueue)
                socket.Dispose();
        }
    }
}
