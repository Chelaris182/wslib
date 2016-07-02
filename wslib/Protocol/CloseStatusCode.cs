namespace wslib.Protocol
{
    public enum CloseStatusCode
    {
        NormalClosure = 1000, // the purpose for which the connection was established has been fulfilled
        GoingAway = 1001, // an endpoint is "going away", such as a server going down or a browser having navigated away from a page
        ProtocolError = 1002,
        UnsupportedDataType = 1003, // the endpoint has received a type of data it cannot accept
        InconsistentData = 1007,
        MessageViolatesPolicy = 1008, // the endpoint has received a message that violates its policy
        MessageTooLarge = 1009, //  the endpoint has received a message that is too big for it to process
        ExtensionRequired = 1010,
        UnexpectedCondition = 1011, // the endpoint encountered an unexpected condition
    }
}