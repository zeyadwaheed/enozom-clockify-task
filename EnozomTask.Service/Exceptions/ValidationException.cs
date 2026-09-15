namespace EnozomTask.Service.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}