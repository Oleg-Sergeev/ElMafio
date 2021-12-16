using System;
using System.Collections.Generic;
using System.Linq;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;



public abstract class GameRole
{
    protected static readonly Random _random = new();


    public virtual IGuildUser Player { get; }

    public virtual IGuildUser? LastMove { get; protected set; }


    public virtual string Name { get; }

    public virtual int VoteTime { get; }


    public virtual bool IsAlive { get; protected set; }

    public virtual bool BlockedByHooker { get; protected set; }

    public virtual bool IsNight { get; protected set; }


    protected bool IsSkip { get; set; }

    protected GameRoleData Data { get; }



    public GameRole(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int voteTime)
    {
        var name = GetType().Name;
        Data = options.Get(name);

        Name = Data.Name;

        VoteTime = voteTime;

        Player = player;

        IsAlive = true;
    }



    public virtual IEnumerable<IGuildUser> GetExceptList()
        => Enumerable.Empty<IGuildUser>();

    public virtual void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        LastMove = !isSkip ? selectedPlayer : null;

        IsSkip = isSkip;
    }



    public virtual ICollection<(EmbedStyle, string)> GetMoveResultPhasesSequence()
    {
        var sequence = new List<(EmbedStyle, string)>();

        if (LastMove is not null)
        {
            sequence.Add((EmbedStyle.Successfull, "Выбор сделан"));
            sequence.Add((EmbedStyle.Successfull, ParsePattern(GetRandomPhrase(Data.SuccessPhrases), $"**{LastMove?.GetFullName()}**")));
        }
        else
        {
            if (IsSkip)
                sequence.Add((EmbedStyle.Error, "Вы пропустили голосование"));
            else
            {
                sequence.Add((EmbedStyle.Error, "Вы не смогли сделать выбор"));
                sequence.Add((EmbedStyle.Error, ParsePattern(GetRandomPhrase(Data.FailurePhrases))));
            }
        }

        return sequence;
    }

    public string GetRandomYourMovePhrase() => Data.YourMovePhrases[_random.Next(Data.YourMovePhrases.Length)];

    protected static string GetRandomPhrase(string[] phrases) => phrases[_random.Next(phrases.Length)];

    public virtual void GameOver()
    {
        IsAlive = false;
    }


    public virtual void Block(IBlocker byRole)
    {
        switch (byRole)
        {
            case Hooker:
                BlockedByHooker = true;
                break;
        }
    }

    public virtual void Unblock()
    {
        BlockedByHooker = false;
    }

    public virtual void SetPhase(bool isNight)
    {
        IsNight = isNight;

        if (isNight)
            BlockedByHooker = false;
    }


    protected static string ParsePattern(string str, params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
            str = str.Replace($"{{{i}}}", $"**{values[i]}**");

        return str;
    }
}
