using System.Runtime.Serialization;

namespace Core.Exceptions;

public class GameSetupAbortedException : Exception
{
    public GameSetupAbortedException()
    {
    }

    public GameSetupAbortedException(string? message) : base(message)
    {
    }

    public GameSetupAbortedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected GameSetupAbortedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}