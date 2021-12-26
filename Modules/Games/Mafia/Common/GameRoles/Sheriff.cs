using System.Collections.Generic;
using System.Linq;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;


public class Sheriff : Innocent, IKiller, IChecker
{
    public bool IsAvailableToShot => ShotsCount > 0;


    public IGuildUser? KilledPlayer { get; protected set; }

    public GameRole? CheckedRole { get; protected set; }


    protected int ShotsCount { get; set; }

    protected bool WasShot { get; set; }

    protected bool ShotSelected { get; set; }


    public IEnumerable<GameRole> CheckableRoles { get; }


    public Sheriff(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int maxShotsCount, IEnumerable<Murder> murders) : base(player, options)
    {
        ShotsCount = maxShotsCount;

        CheckableRoles = murders;
    }

    public override void SetPhase(bool isNight)
    {
        base.SetPhase(isNight);

        if (isNight)
        {
            CheckedRole = null;
            KilledPlayer = null;
        }
    }


    public override IEnumerable<IGuildUser> GetExceptList()
        => new List<IGuildUser>()
        {
            Player
        };


    public override ICollection<(EmbedStyle, string)> GetMoveResultPhasesSequence()
    {
        var sequence = base.GetMoveResultPhasesSequence();

        if (LastMove is not null)
        {
            var sheriffData = (SheriffData)Data;

            if (ShotSelected)
            {
                if (KilledPlayer is not null)
                    sequence.Add((EmbedStyle.Successfull, ParsePattern(GetRandomPhrase(sheriffData.SuccessfullKillPhrases), $"**{LastMove.GetFullName()}**")));
                else
                    sequence.Add((EmbedStyle.Error, ParsePattern(GetRandomPhrase(sheriffData.FailedKillPhrases))));
            }
            else
            {
                if (CheckedRole is not null)
                    sequence.Add((EmbedStyle.Successfull, ParsePattern(GetRandomPhrase(sheriffData.SuccessfullCheckPhrases), $"**{LastMove.GetFullName()}**", $"**{CheckedRole.Name}**")));
                else
                    sequence.Add((EmbedStyle.Error, ParsePattern(GetRandomPhrase(sheriffData.FailedCheckPhrases), $"**{LastMove.GetFullName()}**")));
            }
        }

        return sequence;
    }


    public void ConfigureMove(bool shotSelected) => ShotSelected = shotSelected;

    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);

        if (!IsNight)
            return;

        if (ShotSelected && ShotsCount > 0)
        {
            CheckedRole = null;

            KilledPlayer = selectedPlayer;

            ShotsCount--;
        }
        else
        {
            KilledPlayer = null;

            CheckedRole = CheckableRoles.FirstOrDefault(r => r.Player == selectedPlayer);
        }
    }
}
