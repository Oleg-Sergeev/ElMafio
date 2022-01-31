namespace Core.Exceptions;

public class WrongPlayersCountException : Exception
{
    public WrongPlayersCountException()
    {
    }

    public WrongPlayersCountException(string? message) : base(message)
    {
    }

    public WrongPlayersCountException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public WrongPlayersCountException(string? message, int expected, int actual) : base($"{message}\nExpected: {expected}\nActual:{actual}")
    {

    }
}
