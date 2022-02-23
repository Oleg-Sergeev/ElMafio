using Discord;

namespace Core.Extensions;

public static class ColorExtensions
{
    public static string ToRgbString(this Color color)
        => $"({color.R}, {color.G}, {color.B})";
}
