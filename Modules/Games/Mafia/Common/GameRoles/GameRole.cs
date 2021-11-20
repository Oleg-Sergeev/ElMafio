using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public abstract class GameRole
{
    protected static readonly Random _random = new();

    public virtual IGuildUser Player { get; init; }

    public virtual IGuildUser? LastMove { get; protected set; }

    public virtual string Name { get; init; }

    public virtual int VoteTime { get; init; }

    public virtual bool IsAlive { get; protected set; }

    public virtual bool CanDoMove { get; protected set; }


    protected GameRoleData Data { get; }


    public GameRole(IGuildUser player, IOptionsMonitor<GameRoleData> options, int voteTime)
    {
        Data = options.Get(GetType().Name);

        Name = Data.Name;

        VoteTime = voteTime;

        Player = player;

        IsAlive = true;

        CanDoMove = true;
    }


    public virtual IEnumerable<IGuildUser> GetExceptList()
        => Enumerable.Empty<IGuildUser>();

    public virtual void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        LastMove = !isSkip ? selectedPlayer : null;
    }


    public string GetRandomPhrase(bool isSuccess) => isSuccess
        ? Data.SuccessPhrases[_random.Next(Data.SuccessPhrases.Length)]
        : Data.FailurePhrases[_random.Next(Data.FailurePhrases.Length)];


    public void BlockMove() => CanDoMove = false;

    public void UnblockMove() => CanDoMove = true;

    public void GameOver()
    {
        IsAlive = false;
        LastMove = null;
    }
}
