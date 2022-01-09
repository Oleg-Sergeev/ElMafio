﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;


public class Sheriff : Innocent, IKiller, IChecker
{
    public IGuildUser? KilledPlayer { get; protected set; }

    public GameRole? CheckedRole { get; protected set; }


    protected int ShotsCount { get; set; }

    protected bool ShotSelected { get; set; }


    public IEnumerable<GameRole> CheckableRoles { get; }


    public Sheriff(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int maxShotsCount, IEnumerable<Murder> murders) : base(player, options)
    {
        ShotsCount = maxShotsCount;

        CheckableRoles = murders;
    }


    protected override IEnumerable<IGuildUser> GetExceptList()
        => !IsNight ? base.GetExceptList()
        : new List<IGuildUser>()
        {
            Player
        };


    // ******************
    public override ICollection<(EmbedStyle, string)> GetMoveResultPhasesSequence()
    {
        var sequence = base.GetMoveResultPhasesSequence();

        if (LastMove is not null)
        {
            if (ShotSelected)
            {
                if (KilledPlayer is not null)
                    sequence.Add((EmbedStyle.Successfull, ParsePattern(GetRandomPhrase(Data.Phrases[IKiller.SuccessfullKillPhrases]), $"**{LastMove.GetFullName()}**")));
                else
                    sequence.Add((EmbedStyle.Error, ParsePattern(GetRandomPhrase(Data.Phrases[IKiller.FailedKillPhrases]))));
            }
            else
            {
                if (CheckedRole is not null)
                    sequence.Add((EmbedStyle.Successfull, ParsePattern(GetRandomPhrase(Data.Phrases[IChecker.SuccessfullCheckPhrases]), $"**{LastMove.GetFullName()}**", $"**{CheckedRole.Name}**")));
                else
                    sequence.Add((EmbedStyle.Error, ParsePattern(GetRandomPhrase(Data.Phrases[IChecker.FailedCheckPhrases]), $"**{LastMove.GetFullName()}**")));
            }
        }

        return sequence;
    }


    protected override void HandleChoice(IGuildUser? choice)
    {
        base.HandleChoice(choice);

        if (!IsNight)
            return;

        if (ShotSelected && ShotsCount > 0)
        {
            CheckedRole = null;

            KilledPlayer = choice;

            ShotsCount--;
        }
        else
        {
            KilledPlayer = null;

            CheckedRole = choice is null ? null : CheckableRoles.FirstOrDefault(r => r.Player == choice);
        }
    }


    public async override Task<Vote> VoteAsync(MafiaContext context, CancellationToken token, IUserMessage? message = null)
    {
        if (!IsNight)
            return await base.VoteAsync(context, token, message);


        ShotSelected = false;

        if (ShotsCount > 0)
        {
            var pageBuilder = new PageBuilder()
                .WithTitle("");


            var selection = new SelectionBuilder<bool>()
                .WithOptions(new List<bool> { true, false });


            //ShotSelected = true;
        }

        var vote = await base.VoteAsync(context, token, message);


        // TODO: Remove from here

        if (!ShotSelected)
        {
            var seq = GetMoveResultPhasesSequence();

            foreach (var phrase in seq)
            {
                await Player.SendMessageAsync(embed: EmbedHelper.CreateEmbed(phrase.Item2, phrase.Item1));
            }
        }


        return vote;
    }
}
