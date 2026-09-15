namespace EnozomTask.Service.Exceptions;

public class ClockifyApiException : Exception
{
    public int? RemoteStatusCode { get; }

    public ClockifyApiException(string message, int? remoteStatusCode = null)
        : base(message)
    {
        RemoteStatusCode = remoteStatusCode;
    }
}
