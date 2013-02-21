namespace BrightstarDB.Cluster.Common
{
    public class Response
    {
        public bool CloseConnection { get; private set; }
        public Message ResponseMessage { get; private set; }

        public Response(Message responseMessage) : this(responseMessage, false)
        {
        }

        public Response(Message responseMessage, bool closeConnectionAfterSend)
        {
            CloseConnection = closeConnectionAfterSend;
            ResponseMessage = responseMessage;
        }

        public override string ToString()
        {
            return ResponseMessage.ToString();
        }
    }
}