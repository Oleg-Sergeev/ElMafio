using System.Collections.Generic;
using System.Linq;
using Core.Common;
using Core.Extensions;
using Discord;
using Infrastructure.Data.Entities.Games.Stats;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Don : Murder, IChecker
{
    public int RevealsCount { get; set; }

    public GameRole? CheckedRole { get; protected set; }

    public IEnumerable<GameRole> CheckableRoles { get; }


    protected bool IsChecking { get; set; }



    public Don(IGuildUser player, IOptionsSnapshot<GameRoleData> options, IEnumerable<Sheriff> sheriffs) : base(player, options)
    {
        CheckableRoles = sheriffs;
    }


    protected override IEnumerable<IGuildUser> GetExceptList()
        => !IsChecking
        ? base.GetExceptList()
        : new List<IGuildUser>
        {
            Player
        };


    public override ICollection<(EmbedStyle, string)> GetMoveResultPhasesSequence()
    {
        var sequence = base.GetMoveResultPhasesSequence();

        if (LastMove is not null)
        {
            if (CheckedRole is not null)
                sequence.Add((EmbedStyle.Successfull, ParsePattern(GetRandomPhrase(Data.Phrases[IChecker.SuccessfullCheckPhrases]), $"**{LastMove.GetFullName()}**", $"**{CheckedRole.Name}**")));
            else
                sequence.Add((EmbedStyle.Error, ParsePattern(GetRandomPhrase(Data.Phrases[IChecker.FailedCheckPhrases]), $"**{LastMove.GetFullName()}**")));
        }


        return sequence;
    }


    public override void SetPhase(bool isNight)
    {
        base.SetPhase(isNight);

        if (!isNight)
            IsChecking = false;
    }


    public override void HandleChoice(IGuildUser? choice)
    {
        if (!IsChecking)
            base.HandleChoice(choice);
        else
        {
            MovesCount++;

            HandleChoiceInternal(this, choice);

            CheckedRole = choice is null ? null : CheckableRoles.FirstOrDefault(r => r.Player.Id == choice.Id);

            if (CheckedRole is not null)
                RevealsCount++;
        }

        if (IsNight && !IsChecking)
        {
            IsChecking = true;
        }
    }


    public override void UpdateStats(MafiaStats stats, Winner winner)
    {
        base.UpdateStats(stats, winner);

        stats.DonMovesCount += MovesCount;
        stats.DonRevealsCount += RevealsCount;
    }
}
