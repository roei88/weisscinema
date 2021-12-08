using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using WebCom;
using System.Reflection;
using ProtoMessages;

namespace Main
{

    public class MessageHandlersManager
    {
        Dictionary<Type, ISessionManager> _messageServers;

        struct HandlerInfo // for new-format messages
        {
            public object Handler;          // instance of a message handler class
            public MethodInfo Method;       // its HandleMessage() method
            public MessageParser Parser;    // Google.Protobuf.MessageParser for a specific Message type
        }

        Dictionary<Type, HandlerInfo> _handlers = new Dictionary<Type, HandlerInfo>();

        public MessageHandlersManager()
        {
            InitCustomMessageHandlers();           
        }

        public bool Start(int port)
        {
            _messageServers = new Dictionary<Type, ISessionManager>();
            ProtobufWebSocketServer protoWSS = new ProtobufWebSocketServer();
            protoWSS.DataReceived += HandleWrappedMessage;
            bool success = protoWSS.Start(port);
            if (!success)
            {
                return false;
            }
            _messageServers.Add(protoWSS.GetType(), protoWSS);
            return true;
        }

        public void Close()
        {
            Console.WriteLine("Closing Message Handler");
            foreach (ISessionManager server in _messageServers.Values)
            {
                Console.WriteLine("closing server " + server.ToString());
                server.CloseConnection();
            }
        }

        void InitCustomMessageHandlers()
        {
            try
            {
                object[] handlerArgs = { this };

                // find all the classes with [MessageHandlerClass] attribute
                var handlerClassTypes = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.GetCustomAttribute<MessageHandlerClassAttribute>(true) != null)
                    .ToList();

                // for each class: find the message handler methods and store them as Delegates
                foreach (Type handlerClassType in handlerClassTypes)
                {
                    object handler = Activator.CreateInstance(handlerClassType, handlerArgs);   // all the handlers receive 'MessageHandler' in constructor

                    var handleMessageMethods = handlerClassType.GetMethods()
                        .Where(m => m.GetCustomAttribute<NewFormatMessageHandlerMethodAttribute>(true) != null)
                        .ToList();

                    foreach (var handleMessageMethod in handleMessageMethods)
                    {
                        ParameterInfo[] methodParams = handleMessageMethod.GetParameters();
                        if (methodParams.Length > 0)
                        {
                            ParameterInfo paramInfo = methodParams[0];
                            Type messageType = paramInfo.ParameterType;
                            PropertyInfo parserProperty = messageType.GetProperty("Parser"); // get a parser for this type of message
                            MessageParser parser = parserProperty.GetMethod.Invoke(null, null) as MessageParser;
                            if (parser == null)
                            {
                                Console.WriteLine($"Failed to get a parser of {messageType} messages");
                                continue;
                            }

                            HandlerInfo handlerInfo = new HandlerInfo { Handler = handler, Method = handleMessageMethod, Parser = parser };
                            _handlers.Add(messageType, handlerInfo);
                            Console.WriteLine($"Added new-format message handler with message type: '{messageType}', handler type: '{handlerClassType}', method: '{handleMessageMethod}'");
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message} Trace: {ex.StackTrace}");
            }
        }

        //Communications

        public void SendWrappedMessage(IMessage message, string sessionId = "")
        {
            WrappedMessage wrappedMessage = new WrappedMessage
            {
                MessageTypeStr = message.GetType().ToString(),
                MessageContent = message.ToByteString()
            };
            SendMessage(wrappedMessage);
        }

        public void SendMessage(IMessage message, string sessionId = "", IDictionary<string, object> headers = null, Type serverType = null)
        {
            if (serverType != null && _messageServers.ContainsKey(serverType))
            {
                _messageServers[serverType].Send(message, sessionId);
            }
            else
            {
                foreach (ISessionManager server in _messageServers.Values)
                {
                    server.Send(message, sessionId);
                }
            }
        }

        //Messages handling

        void HandleWrappedMessage(object sender, byte[] message)
        {
            try
            {
                WrappedMessage wrappedMessage = WrappedMessage.Parser.ParseFrom(message, 0, message.Length);
                if (!string.IsNullOrEmpty(wrappedMessage.MessageTypeStr))
                {
                    string normalizedTypeStr = GetNormalizedMessageTypeString(wrappedMessage.MessageTypeStr);
                    HandleTypedMessage(wrappedMessage.MessageContent.ToByteArray(), normalizedTypeStr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message} Trace: {ex.StackTrace}");
            }
        }

        void HandleTypedMessage(byte[] messageContent, string normalizedTypeStr)
        {
            Type msgType = GetType(normalizedTypeStr);

            if (msgType == null)
            {
                return;
            }

            if (_handlers.ContainsKey(msgType) == false)
            {
                Console.WriteLine($"Cannot find handler for message of type {msgType}");
                return;
            }

            HandlerInfo handlerInfo = _handlers[msgType];
            IMessage msg = handlerInfo.Parser.ParseFrom(messageContent);
            if (msg == null)
            {
                Console.WriteLine($"Failed to parse a message of type {msgType}");
                return;
            }

            Console.WriteLine($"Found handler for message of type {msgType}");
            handlerInfo.Method.Invoke(handlerInfo.Handler, new object[] { msg });
        }

        string GetNormalizedMessageTypeString(string messageTypeStr)
        {
            string normalizedStr = messageTypeStr.Replace("::", ".");
            normalizedStr = normalizedStr.Replace("_", "");
            return normalizedStr;
        }

        Type GetTypeFromAssemblies(string typeName)
        {
            foreach (var assemblie in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assemblie.GetType(typeName,false,true);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        Type GetType(string typeName, bool includeAssemblies = true)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }
            if (includeAssemblies)
            {
                return GetTypeFromAssemblies(typeName);
            }
            return null;
        }
    }
}
