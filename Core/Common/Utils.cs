﻿using Discord;

namespace Core.Common;

public static class Utils
{
    public static Color GetRandomColor()
        => new (Random.Shared.Next(0, 256), Random.Shared.Next(0, 256), Random.Shared.Next(0, 256));
}
