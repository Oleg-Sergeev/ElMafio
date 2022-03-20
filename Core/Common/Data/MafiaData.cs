using Discord;

namespace Core.Common.Data;

public class MafiaData : GameData
{
    public CancellationTokenSource TokenSource { get; private set; }

    public List<IGuildUser> Spectators { get; }

    public MafiaData(string name, int minPlayersCount, IGuildUser host) : base(name, minPlayersCount, host)
    {
        TokenSource = new();

        Spectators = new();
    }


    public void RefreshToken()
    {
        if (!TokenSource.IsCancellationRequested)
            return;

        TokenSource.Dispose();

        TokenSource = new();
    }
}
