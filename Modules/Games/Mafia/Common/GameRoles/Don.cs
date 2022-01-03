using System.Collections.Generic;
using System.Linq;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Don : Murder, IChecker
{
    public GameRole? CheckedRole { get; protected set; }

    public bool IsChecking { get; protected set; }


    public IEnumerable<GameRole> CheckableRoles { get; }


    public Don(IGuildUser player, IOptionsSnapshot<GameRoleData> options, IEnumerable<Sheriff> sheriffs) : base(player, options)
    {
        CheckableRoles = sheriffs;
    }


    public override IEnumerable<IGuildUser> GetExceptList()
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

    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        if (!IsChecking)
            base.ProcessMove(selectedPlayer, isSkip);
        else
        {
            LastMove = !isSkip ? selectedPlayer : null;

            IsSkip = isSkip;


            CheckedRole = CheckableRoles.FirstOrDefault(r => r.Player == selectedPlayer);
        }
    }

    public override void SetPhase(bool isNight)
    {
        base.SetPhase(isNight);

        if (!isNight)
            SetChecking(false);
    }


    public void SetChecking(bool cheking) => IsChecking = cheking;
}
