using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;


public class Sheriff : Innocent, IKiller, IChecker
{
    public int RevealsCount { get; set; }


    public int KillsCount { get; set; }

    public int MovesCount { get; set; }


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


    public override void HandleChoice(IGuildUser? choice)
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
            MovesCount++;

            KilledPlayer = null;

            CheckedRole = choice is null ? null : CheckableRoles.FirstOrDefault(r => r.Player.Id == choice.Id);

            if (CheckedRole is not null)
                RevealsCount++;
        }
    }


    public override void UpdateStats(MafiaStats stats, Winner winner)
    {
        base.UpdateStats(stats, winner);


        stats.SheriffMovesCount += MovesCount;
        stats.SheriffRevealsCount += RevealsCount;
    }


    public override async Task<Vote> VoteAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultChannel = null, bool waitAfterVote = true)
    {
        if (!IsNight)
            return await base.VoteAsync(context, voteChannel, voteResultChannel, waitAfterVote);


        ShotSelected = false;

        if (ShotsCount > 0)
        {
            var pageBuilder = new PageBuilder()
                .WithTitle("Выберите ваше действие");


            var selection = new SelectionBuilder<bool>()
                .WithSelectionPage(pageBuilder)
                .WithOptions(new List<bool> { true, false })
                .WithStringConverter(o => o ? "Выстрел" : "Проверка")
                .Build();


            var result = await context.Interactive.SendSelectionAsync(selection, await Player.CreateDMChannelAsync(),
                TimeSpan.FromSeconds(context.VoteTime / 2), cancellationToken: context.MafiaData.TokenSource.Token);

            if (result.IsSuccess)
                ShotSelected = result.Value;
            else
                ShotSelected = false;
        }


        var vote = await VoteInternalAsync(this, context, voteChannel, voteResultChannel);


        return vote;
    }
}
