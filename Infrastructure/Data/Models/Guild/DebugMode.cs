using System;

namespace Infrastructure.Data.Models.Guild;

public enum DebugMode
{
    Off = 0,
    ErrorMessages = 1 << 1,
    StackTrace = 1 << 2
}
