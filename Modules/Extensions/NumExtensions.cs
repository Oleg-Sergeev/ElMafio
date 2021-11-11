namespace Modules.Extensions;

public static class NumExtensions
{
    public static string ToPercent(this float num) => $"{num * 100:F2}%";
}