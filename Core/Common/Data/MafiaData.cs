using Discord;

namespace Core.Common.Data;

public class MafiaData : GameData
{
    public CancellationTokenSource TokenSource { get; }


    public MafiaData(string name, int minPlayersCount, IGuildUser host, CancellationTokenSource tokenSource) : base(name, minPlayersCount, host)
    {
        TokenSource = tokenSource;
    }
}
