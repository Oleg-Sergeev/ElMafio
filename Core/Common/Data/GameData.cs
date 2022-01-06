using Discord;

namespace Core.Common.Data;


public class GameData
{
    public string Name { get; }

    public bool IsPlaying { get; set; }

    public int MinPlayersCount { get; }

    public IGuildUser Host { get; set; }

    public List<IGuildUser> Players { get; }



    public GameData(string name, int minPlayersCount, IGuildUser host)
    {
        Name = name;

        Host = host;

        MinPlayersCount = minPlayersCount;


        IsPlaying = false;

        Players = new();
    }
}