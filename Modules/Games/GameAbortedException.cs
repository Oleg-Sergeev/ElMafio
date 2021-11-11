using System;
using System.Runtime.Serialization;

namespace Modules.Games;

public class GameAbortedException : Exception
{
    public GameAbortedException()
    {
    }

    public GameAbortedException(string? message) : base(message)
    {
    }

    public GameAbortedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected GameAbortedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}