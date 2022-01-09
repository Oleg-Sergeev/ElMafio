using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Don : Murder, IChecker
{
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


    protected override void HandleChoice(IGuildUser? choice)
    {
        if (!IsChecking)
            base.HandleChoice(choice);
        else
        {
            CheckedRole = choice is null ? null : CheckableRoles.FirstOrDefault(r => r.Player == choice);
        }
    }


    // ***************************
    public override async Task<Vote> VoteAsync(MafiaContext context, CancellationToken token, IUserMessage? message = null)
    {
        if (!IsNight || !IsChecking)
        {
            if (IsNight)
                IsChecking = true;

            return await base.VoteAsync(context, token, message);
        }


        // Select checked role





        return null!;
    }
}
