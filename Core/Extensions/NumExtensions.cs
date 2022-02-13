namespace Core.Extensions;

public static class NumExtensions
{
    public static TimeSpan ToTimeSpanSeconds(this double num) => TimeSpan.FromSeconds(num);

    public static TimeSpan ToTimeSpanMinutes(this double num) => TimeSpan.FromMinutes(num);

    public static TimeSpan ToTimeSpanHours(this double num) => TimeSpan.FromHours(num);
}