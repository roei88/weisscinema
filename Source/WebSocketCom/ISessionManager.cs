using Google.Protobuf;

namespace WebCom
{
    public delegate void DataReceivedDelegate(object sender, byte[] message);
    public delegate void MessageReceivedDelegate(object sender, string message);
    public delegate void SessionDelegate(object sender = null, string id = "");

    public interface ISessionManager
    {
        void Send(string message, string sessionId = "");
        bool Send(IMessage message, string sessionId = "");
        bool Send(byte[] message, string sessionId = "");

        void CloseConnection();

        event DataReceivedDelegate DataReceived;
        event MessageReceivedDelegate MessageReceived;
        event SessionDelegate NewSessionConnected;
        event SessionDelegate SessionDisconnected;
    }
}
