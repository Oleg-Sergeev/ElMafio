namespace Core.Interfaces;

public interface IRandomService
{
    int Next();
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
}
