using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Fergun.Interactive;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Modules.Games.Mafia.Common;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.GameRoles.RolesGroups;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia;

public class MafiaGame
{
    private readonly MafiaContext _context;

    private readonly InteractiveService _interactive;

    private readonly MafiaSettings _settings;
    private readonly MafiaGuildData _guildData;
    private readonly MafiaRolesData _rolesData;

    private readonly OverwritePermissions _denyView;
    private readonly OverwritePermissions _denyWrite;
    private readonly OverwritePermissions _allowWrite;
    private readonly OverwritePermissions? _allowSpeak;

    private readonly CancellationToken _token;


    private bool _isZeroDay;

    public MafiaGame(MafiaContext context)
    {
        _isZeroDay = true;

        _context = context;

        _token = context.MafiaData.TokenSource.Token;

        _settings = context.Settings;
        _guildData = context.GuildData;
        _rolesData = context.RolesData;
        _interactive = context.Interactive;

        _denyView = MafiaHelper.DenyView;
        _denyWrite = MafiaHelper.GetDenyWrite(_guildData.GeneralTextChannel);
        _allowWrite = MafiaHelper.GetAllowWrite(_guildData.GeneralTextChannel);

        if (_guildData.GeneralVoiceChannel is not null)
            _allowSpeak = MafiaHelper.GetAllowSpeak(_guildData.GeneralVoiceChannel);
    }

    public async Task<Winner> RunAsync()
    {
        if (_settings.Current.GameSubSettings.IsCustomGame && _settings.Current.PreGameMessage is not null)
        {
            await _guildData.GeneralTextChannel.SendEmbedAsync(_settings.Current.PreGameMessage, "Сообщение перед игрой");

            await Task.Delay(5000);
        }


        await _guildData.GeneralTextChannel.SendMessageAsync(embed: GenerateRolesInfoEmbed());

        await Task.Delay(5000);


        if (_rolesData.Murders.Count > 1 && _settings.Current.RolesExtraInfoSubSettings.MurdersKnowEachOther)
        {
            var meetTime = _rolesData.Murders.Count * 10;

            await _guildData.GeneralTextChannel.SendEmbedAsync($"Пока город спит, мафия знакомятся друг с другом. Через {meetTime}с город проснется", EmbedStyle.Waiting);

            await IntroduceMurdersAsync(meetTime);
        }


        await _guildData.GeneralTextChannel.SendEmbedAsync($"Приятной игры", "Мафия");

        await Task.Delay(5000);

        Winner? winner;

        var lastWordNightCount = _settings.Current.GameSubSettings.LastWordNightCount;

        try
        {
            while (true)
            {
                winner = await LoopAsync(lastWordNightCount--);

                if (winner is not null)
                    break;
            }

            await _guildData.GeneralTextChannel.SendEmbedAsync("Игра завершена", "Мафия");

            await Task.Delay(3000);

            return winner;
        }
        catch (OperationCanceledException)
        {
            return Winner.None;
        }
        finally
        {
            await ReturnPlayersDataAsync();
        }
    }



    private async Task<Winner?> LoopAsync(int lastWordNightCount)
    {
        await ChangeMurdersPermsAsync(_denyWrite, _denyView);

        await _guildData.GeneralTextChannel.SendMessageAsync($"{_guildData.MafiaRole.Mention} Доброе утро, жители города! Самое время пообщаться всем вместе.");

        await Task.Delay(3000);

        var lastWordTasks = new List<Task<string>>();

        if (!_isZeroDay)
        {
            await _guildData.GeneralTextChannel.SendMessageAsync($"Но сначала новости: сегодня утром, в незаправленной постели...");

            var delay = Task.Delay(3000, _token);


            var corpses = GetCorpses(out var revealedManiacs);


            var msg = "";
            if (corpses.Count > 0)
            {
                msg += corpses.Count == 1 ? "Был обнаружен труп:\n" : "Были обнаружены трупы:\n";

                for (int i = 0; i < corpses.Count; i++)
                    msg += $"{corpses[i].Mention}\n";
            }
            else
                msg = "Никого не оказалось. Все живы.";


            await delay;

            await _guildData.GeneralTextChannel.SendMessageAsync(msg);


            if (revealedManiacs.Count > 0)
            {
                var str = "Слухи доносят, что следующие игроки являются маньяками:";

                for (int i = 0; i < revealedManiacs.Count; i++)
                {
                    str += $"\n{revealedManiacs[i].GetFullMention()}";
                }

                if (_settings.Current.RolesExtraInfoSubSettings.MurdersKnowEachOther)
                    await _guildData.MurderTextChannel.SendMessageAsync(str);
                else
                    await _guildData.GeneralTextChannel.SendMessageAsync(str);
            }


            for (int i = 0; i < corpses.Count; i++)
            {
                await EjectPlayerAsync(corpses[i]);


                if (lastWordNightCount > 0)
                {
                    var task = SendLastWordMessageAsync(corpses[i]);

                    lastWordTasks.Add(task);
                }
            }


            var winner = DetermineWinner();

            if (winner is not null)
                return winner;
        }


        var dayTime = _rolesData.AliveRoles.Values.Count * 20;


        await _guildData.GeneralTextChannel.SendEmbedAsync($"Обсуждайте ({dayTime}с)");

        await ChangeCitizenPermsAsync(_allowWrite, _allowSpeak);


        var timer = WaitForTimerAsync(dayTime, _guildData.GeneralTextChannel);


        foreach (var role in _rolesData.AliveRoles.Values)
        {
            role.SetPhase(false);
        }


        while (lastWordTasks.Count > 0)
        {
            var receivedMessageTask = await Task.WhenAny(lastWordTasks);

            lastWordTasks.Remove(receivedMessageTask);

            var lastWord = await receivedMessageTask;


            await _guildData.GeneralTextChannel.SendEmbedAsync(lastWord);
        }


        var fooledPlayerIds = new List<ulong?>();
        foreach (var hooker in _rolesData.Hookers.Values.Where(h => h.IsAlive))
            if (hooker.Votes.Count > 0)
                fooledPlayerIds.Add(hooker.Votes[^1].Option?.Id);


        await timer;

        await ChangeCitizenPermsAsync(_denyWrite, _denyView);


        if (_isZeroDay)
        {
            _isZeroDay = false;
        }
        else
        {
            var citizenVotingResult = await DoCitizenVotingAsync();

            if (citizenVotingResult.Choice.Option is not null)
            {
                var role = _rolesData.AliveRoles[citizenVotingResult.Choice.Option];

                if (!fooledPlayerIds.Contains(role.Player.Id))
                {
                    await EjectPlayerAsync(role.Player);
                }
                else
                {
                    await _guildData.GeneralTextChannel.SendEmbedAsync($"{role.Player.Mention} не покидает игру из-за наличия алиби", EmbedStyle.Warning);
                }
            }
            else
            {
                if (citizenVotingResult.Choice.IsSkip)
                    await _guildData.GeneralTextChannel.SendEmbedAsync("Пропуск", EmbedStyle.Successfull);
                else
                    await _guildData.GeneralTextChannel.SendEmbedAsync("Не удалось выбрать", EmbedStyle.Error);
            }


            var winner = DetermineWinner();

            if (winner is not null)
                return winner;
        }


        await _guildData.GeneralTextChannel.SendEmbedAsync("**Город засыпает**");



        foreach (var role in _rolesData.AliveRoles.Values)
        {
            role.SetPhase(true);
        }

        await DoNightMovesAsync();


        return DetermineWinner();
    }


    private async Task<VoteGroup> DoCitizenVotingAsync()
    {
        var citizen = _rolesData.GroupRoles[nameof(CitizenGroup)];

        var votingResult = await citizen.VoteManyAsync(_context, waitAfterVote: false);

        return votingResult;
    }

    private async Task DoNightMovesAsync()
    {
        var exceptRoles = new List<GameRole>(_rolesData.Murders.Values.Where(m => m is not Don));

        var roles = _rolesData.AllRoles.Values.ToList();


        //handle specific roles

        if (!_settings.Current.GameSubSettings.IsCustomGame || _settings.Current.RolesExtraInfoSubSettings.MurdersKnowEachOther)
            await ChangeMurdersPermsAsync(_allowWrite, _allowSpeak);

        if (!_settings.Current.GameSubSettings.IsCustomGame || _settings.Current.RolesExtraInfoSubSettings.MurdersVoteTogether)
        {
            var murdersGroup = _rolesData.GroupRoles[nameof(MurdersGroup)];

            roles.Add(murdersGroup);
        }
        else
            foreach (var murder in _rolesData.Murders.Values)
                exceptRoles.Remove(murder);


        roles = roles.Except(exceptRoles).ToList();

        var priorityGroups = roles.GroupBy(r => r.Priority).OrderByDescending(g => g.Key);

        int i = 1;
        foreach (var rolesGrouping in priorityGroups)
        {
            var uniqueRoles = rolesGrouping.DistinctBy(r => r.Name);

            var str = "Сейчас ход делают:\n" + string.Join('\n', uniqueRoles.Select(r => $"**{r.Name}**"));

            await _guildData.GeneralTextChannel.SendEmbedAsync(str, EmbedStyle.Waiting, $"Очередь #{i}");


            var tasksSingle = new List<Task<Vote>>();
            var tasksGroup = new List<Task<VoteGroup>>();

            foreach (var role in rolesGrouping)
            {
                if (role is GroupRole rolesGroup)
                    tasksGroup.Add(rolesGroup.VoteManyAsync(_context));
                else
                    tasksSingle.Add(role.VoteAsync(_context));
            }


            var taskSingle = Task.Run(async () =>
            {
                while (tasksSingle.Count > 0)
                {
                    var task = await Task.WhenAny(tasksSingle);

                    var vote = await task;

                    tasksSingle.Remove(task);

                    if (_guildData.SpectatorTextChannel is not null)
                        await _guildData.SpectatorTextChannel.SendEmbedAsync(
                            $"{(vote.VotedRole.IsAlive ? "" : "**[Dead]** ")}{vote.VotedRole} [{vote.VotedRole.Player.GetFullName()}] - {(vote.IsSkip ? "Skip" : vote.Option?.GetFullName() ?? "None")}",
                            "Голосование");
                }
            });

            var taskGroup = Task.Run(async () =>
            {
                while (tasksGroup.Count > 0)
                {
                    var task = await Task.WhenAny(tasksGroup);

                    var voteGroup = await task;

                    tasksGroup.Remove(task);

                    if (_guildData.SpectatorTextChannel is not null)
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"Голосование [{voteGroup.Choice.VotedRole}]")
                            .AddField("Игрок", string.Join('\n', voteGroup.PlayersVote.Values.Select(vote => $"{vote.VotedRole} [{vote.VotedRole.Player.GetFullName()}]")), true)
                            .AddField("Голос", string.Join('\n', voteGroup.PlayersVote.Values.Select(vote => $"{(vote.IsSkip ? "Skip" : vote.Option?.GetFullName() ?? "None")}")), true)
                            .AddField("Результат", voteGroup.Choice.IsSkip ? "Skip" : voteGroup.Choice.Option?.GetFullName() ?? "None")
                            .Build();

                        await _guildData.SpectatorTextChannel.SendMessageAsync(embed: embed);
                    }
                }
            });


            await Task.WhenAll(taskSingle, taskGroup);


            await _guildData.GeneralTextChannel.SendEmbedAsync("Игроки сделали свои ходы", EmbedStyle.Successfull, $"Очередь #{i}");

            i++;
        }
    }

    private IReadOnlyList<IGuildUser> GetCorpses(out IReadOnlyList<IGuildUser> revealedManiacs)
    {
        var killers = _rolesData.AliveRoles.Values.Where(r => r is IKiller and not Maniac).Cast<IKiller>();
        var healers = _rolesData.AliveRoles.Values.Where(r => r is IHealer).Cast<IHealer>();

        var kills = killers
            .Where(k => k.KilledPlayer is not null)
            .Select(k => k.KilledPlayer!)
            .Distinct();

        var heals = healers
            .Where(h => h.HealedPlayer is not null)
            .Select(k => k.HealedPlayer!)
            .Distinct();


        var corpses = kills
            .Except(heals)
            .Distinct()
            .ToList();


        var maniacKills = _rolesData.Maniacs.Values
            .Where(m => m.IsAlive && m.KilledPlayer is not null)
            .Select(m => m.KilledPlayer!)
            .Except(_rolesData.Hookers.Values
                .Where(h => h.IsAlive && h.HealedPlayer is not null)
                .Select(h => h.HealedPlayer!));

        corpses.AddRange(maniacKills);


        var maniacs = new List<IGuildUser>();

        foreach (var hooker in _rolesData.Hookers.Values)
        {
            if (!hooker.IsAlive || hooker.HealedPlayer is null)
                continue;


            if (corpses.Contains(hooker.Player))
                corpses.Add(hooker.HealedPlayer);

            if (_rolesData.Maniacs.ContainsKey(hooker.HealedPlayer))
                maniacs.Add(hooker.HealedPlayer);
        }


        revealedManiacs = maniacs;

        return corpses
            .Distinct()
            .Shuffle()
            .ToList();
    }




    private async Task<string> SendLastWordMessageAsync(IGuildUser player)
    {
        var dmChannel = await player.CreateDMChannelAsync();

        var msg = await dmChannel.SendMessageAsync(embed: EmbedHelper.CreateEmbed($"{player.Username}, у вас есть 30с для последнего слова, воспользуйтесь этим временем с умом." +
            $" Напишите здесь сообщение, которое увидят все игроки Мафии"));

        var result = await _interactive.NextMessageAsync(m => m.Channel.Id == msg.Channel.Id && m.Author.Id == msg.Author.Id,
            timeout: TimeSpan.FromSeconds(30),
            cancellationToken: _token);

        if (result.IsSuccess)
        {
            await dmChannel.SendEmbedAsync("Сообщение успешно отправлено", EmbedStyle.Successfull);

            return $"{player.GetFullName()} перед смертью сказал следующее:\n{result.Value.Content ?? "*пустое сообщение*"}";
        }
        else
        {
            return $"{player.GetFullName()} умер молча";
        }
    }



    private async Task EjectPlayerAsync(IGuildUser player, bool isKill = true)
    {
        await _guildData.MurderTextChannel.RemovePermissionOverwriteAsync(player);


        _rolesData.AllRoles[player].GameOver();
        _rolesData.AliveRoles.Remove(player);


        await player.RemoveRoleAsync(_guildData.MafiaRole);


        if (_guildData.PlayerRoleIds.TryGetValue(player.Id, out var rolesIds) && rolesIds.Count > 0)
            await player.AddRolesAsync(rolesIds);

        if (_guildData.OverwrittenNicknames.Contains(player.Id))
        {
            await player.ModifyAsync(props => props.Nickname = null);

            _guildData.OverwrittenNicknames.Remove(player.Id);
        }


        if (isKill)
        {
            _guildData.KilledPlayers.Add(player);

            if (_guildData.SpectatorTextChannel is not null && _guildData.SpectatorRole is not null)
                await player.AddRoleAsync(_guildData.SpectatorRole);
        }
    }

    private async Task IntroduceMurdersAsync(int meetTime)
    {
        await ChangeMurdersPermsAsync(_allowWrite, _allowSpeak);


        await _guildData.MurderTextChannel.SendMessageAsync(
            $"{_guildData.MafiaRole.Mention} Добро пожаловать в мафию! Сейчас ночь, город спит. Самое время познакомиться с остальными членами мафии");


        await _guildData.MurderTextChannel.SendMessageAsync(embed: GenerateMurdersListEmbed());


        await WaitForTimerAsync(meetTime, _guildData.GeneralTextChannel, _guildData.MurderTextChannel);

        await _guildData.MurderTextChannel.SendMessageAsync("Время вышло! Переходите в общий канал и старайтесь не подавать виду, что вы мафиозник.");

        await Task.Delay(3000);
    }


    private async Task ChangeMurdersPermsAsync(OverwritePermissions textPerms, OverwritePermissions? voicePerms)
    {
        foreach (var murder in _rolesData.Murders.Values)
        {
            if (!murder.IsAlive)
                continue;

            var player = murder.Player;

            await _guildData.MurderTextChannel.AddPermissionOverwriteAsync(player, textPerms);


            if (voicePerms is not OverwritePermissions perms)
                continue;

            if (_guildData.MurderVoiceChannel is not null)
                await _guildData.MurderVoiceChannel.AddPermissionOverwriteAsync(player, perms);

            if (player.VoiceChannel != null && perms.ViewChannel == PermValue.Deny)
                await player.ModifyAsync(props => props.Channel = null);
        }
    }

    private async Task ChangeCitizenPermsAsync(OverwritePermissions textPerms, OverwritePermissions? voicePerms)
    {
        await _guildData.GeneralTextChannel.AddPermissionOverwriteAsync(_guildData.MafiaRole, textPerms);


        if (_guildData.GeneralVoiceChannel is null || voicePerms is not OverwritePermissions perms)
            return;

        await _guildData.GeneralVoiceChannel.AddPermissionOverwriteAsync(_guildData.MafiaRole, perms);

        if (perms.ViewChannel == PermValue.Deny)
            foreach (var role in _rolesData.AliveRoles.Values)
            {
                var player = role.Player;

                if (player.VoiceChannel != null)
                    await player.ModifyAsync(props => props.Channel = null);
            }
    }



    private async Task ReturnPlayersDataAsync()
    {
        foreach (var role in _rolesData.AllRoles.Values)
        {
            var player = role.Player;


            await EjectPlayerAsync(player, false);
        }

        if (_guildData.SpectatorTextChannel is not null && _guildData.SpectatorRole is not null)
            foreach (var player in _guildData.KilledPlayers)
                await player.RemoveRoleAsync(_guildData.SpectatorRole);
    }



    public async Task WaitForTimerAsync(int seconds, params IMessageChannel[] channels)
    {
        if (channels.Length == 0)
        {
            await Task.Delay(seconds * 1000, _token);

            return;
        }


        while (seconds >= 30)
        {
            seconds /= 2;

            await Task.Delay(seconds * 1000, _token);

            await NotifyAsync(seconds);
        }

        await Task.Delay((seconds - 10) * 1000, _token);

        seconds -= seconds - 10;

        await NotifyAsync(seconds);


        await Task.Delay((seconds - 3) * 1000, _token);

        seconds -= seconds - 3;


        for (int sec = seconds; sec > 0; sec--)
        {
            await NotifyAsync(sec);

            await Task.Delay(1000, _token);
        }



        Task<IEnumerable<IUserMessage>> NotifyAsync(int secs)
            => _context.GuildData.GeneralTextChannel.BroadcastMessagesAsync(channels, embed: EmbedHelper.CreateEmbed($"Осталось {secs}с", EmbedStyle.Waiting));
    }




    private Winner? DetermineWinner()
    {
        var gameSettings = _settings.Current.GameSubSettings;


        var murdersCount = _rolesData.Murders.Values.Count(m => m.IsAlive);

        var innocentsCount = _rolesData.Innocents.Values.Count(i => i.IsAlive);



        if (gameSettings.IsCustomGame)
        {
            var activeNeutralsCount = _rolesData.Neutrals.Values.Count(n => n.IsAlive && n is IKiller);


            if (innocentsCount == 0)
            {
                if (murdersCount > 0)
                {
                    if (!gameSettings.ConditionContinueGameWithNeutrals || activeNeutralsCount == 0)
                        return GetMurders();

                    //if (murdersCount == 1 && activeNeutralsCount == 1)
                    return Winner.None;

                    //return null;
                }
                else
                {
                    if (activeNeutralsCount > 0)
                        return Winner.None; // concrete neutral win

                    return Winner.None;
                }
            }
            else
            {
                if (murdersCount == 0)
                {
                    if (!gameSettings.ConditionContinueGameWithNeutrals || activeNeutralsCount == 0)
                        return GetCitizen();

                    if (innocentsCount == 1 && activeNeutralsCount == 1)
                        return Winner.None;

                    return null;
                }
                else
                {
                    if (!gameSettings.ConditionAliveAtLeast1Innocent && innocentsCount <= murdersCount)
                        return GetMurders();

                    return null;
                }
            }
        }
        else
        {
            if (murdersCount > 0 && innocentsCount > murdersCount)
                return null;

            if (murdersCount == 0)
                return GetCitizen();

            return GetMurders();
        }




        Winner GetCitizen() => new(_rolesData.GroupRoles[nameof(CitizenGroup)], _rolesData.Innocents.Keys);
        Winner GetMurders() => new(_rolesData.GroupRoles[nameof(MurdersGroup)]);
    }


    private Embed GenerateRolesInfoEmbed()
    {
        var roleGrouping = _context.RolesData.AliveRoles.Values.Select(r => r.Name).GroupBy(n => n, (n, group) => new { Name = n, Count = group.Count() });

        var embed = new EmbedBuilder()
            .WithTitle("Состав игроков")
            .WithColor(Color.DarkOrange)
            .AddField("Роль", string.Join("\n", roleGrouping.Select(r => r.Name)), true)
            .AddField("Кол-во", string.Join("\n", roleGrouping.Select(r => r.Count)), true)
            .Build();

        return embed;
    }

    private Embed GenerateMurdersListEmbed()
        => new EmbedBuilder()
        .WithTitle("Состав мафии")
        .WithColor(Color.DarkRed)
        .AddField("Игрок", string.Join("\n", _rolesData.Murders.Keys.Select(u => u.GetFullMention())), true)
        .AddField("Роль", string.Join("\n", _rolesData.Murders.Values.Select(m => m.Name)), true)
        .Build();
}


public class Winner
{
    public static readonly Winner None = new(null);


    public GameRole? Role { get; }

    public IEnumerable<IGuildUser>? Players;

    public Winner(GameRole? role, IEnumerable<IGuildUser>? players = null)
    {
        Role = role;
        Players = players;
    }
}