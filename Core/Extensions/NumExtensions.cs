namespace Core.Extensions;

public static class NumExtensions
{
    public static string ToPercent(this float num) => $"{num * 100:F2}%";


    public static TimeSpan ToTimeSpanSeconds(this double num) => TimeSpan.FromSeconds(num);
}