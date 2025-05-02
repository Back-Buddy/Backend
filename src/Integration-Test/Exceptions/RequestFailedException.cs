namespace BackBuddy.Integration_Test.Exceptions
{
    public class RequestFailedException(HttpResponseMessage responseMessage) : Exception
    {
        public HttpResponseMessage ResponseMessage { get; } = responseMessage;
    }
}
