using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

    public async Task RunAsync()
    {
        if (_settings.Current.GameSubSettings.IsCustomGame && _settings.Current.PreGameMessage is not null)
        {
            await _guildData.GeneralTextChannel.SendEmbedAsync(_settings.Current.PreGameMessage, "Сообщение перед игрой");

            await Task.Delay(5000);
        }

        await _guildData.GeneralTextChannel.SendMessageAsync(embed: GenerateRolesInfoEmbed());

        await Task.Delay(5000);


        if (_rolesData.Murders.Count > 1 && _settings.Current.RolesInfoSubSettings.MurdersKnowEachOther)
        {
            var meetTime = _rolesData.Murders.Count * 10;

            await _guildData.GeneralTextChannel.SendEmbedAsync($"Пока город спит, мафия знакомятся друг с другом. Через {meetTime}с город проснется", EmbedStyle.Waiting);

            await IntroduceMurdersAsync(meetTime);
        }


        await _guildData.GeneralTextChannel.SendEmbedAsync($"Приятной игры", "Мафия");

        await Task.Delay(5000);

        try
        {
            while (!_context.MafiaData.TokenSource.IsCancellationRequested && _rolesData.AliveRoles.Count > 2)
            {
                await LoopAsync();
            }
        }
        catch (OperationCanceledException)
        {
            await _guildData.GeneralTextChannel.SendEmbedAsync("Михалыч дернул ручник", EmbedStyle.Warning);
        }
        finally
        {
            await ReturnPlayersDataAsync();
        }
    }



    private async Task LoopAsync()
    {
        await ChangeMurdersPermsAsync(_denyWrite, _denyView);

        await _guildData.GeneralTextChannel.SendMessageAsync($"{_guildData.MafiaRole.Mention} Доброе утро, жители города! Самое время пообщаться всем вместе.");

        await Task.Delay(3000);

        if (!_isZeroDay)
        {
            await _guildData.GeneralTextChannel.SendMessageAsync($"Но сначала новости: сегодня утром, в незаправленной постели...");

            var delay = Task.Delay(3000, _token);


            var corpses = GetCorpses();


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


            for (int i = 0; i < corpses.Count; i++)
            {
                await EjectPlayerAsync(corpses[i]);
            }
        }


        var dayTime = _rolesData.AliveRoles.Values.Count * 20;


        await _guildData.GeneralTextChannel.SendEmbedAsync($"Обсуждайте ({dayTime}с)");

        await ChangeCitizenPermsAsync(_allowWrite, _allowSpeak);

        var timer = WaitForTimerAsync(dayTime, _guildData.GeneralTextChannel);

        // extra logic

        await timer;


        await ChangeCitizenPermsAsync(_denyWrite, _denyView);


        if (_isZeroDay)
        {
            _isZeroDay = false;
        }
        else
        {
            var votingMessage = await _guildData.GeneralTextChannel.SendEmbedAsync("Голосование начинается...", EmbedStyle.Waiting);

            await Task.Delay(3000);

            var citizenVotingResult = await DoCitizenVotingAsync(votingMessage);

            if (citizenVotingResult.Choice.Option is not null)
            {
                var role = _rolesData.AliveRoles[citizenVotingResult.Choice.Option];

                if (!role.BlockedByHooker)
                {
                    await EjectPlayerAsync(role.Player);

                    await _guildData.GeneralTextChannel.SendEmbedAsync($"Выгнан {role.Player.GetFullMention()}", EmbedStyle.Successfull);
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
        }


        await _guildData.GeneralTextChannel.SendEmbedAsync("**Город засыпает**");


        await DoNightMovesAsync();
    }


    private async Task<VoteGroup> DoCitizenVotingAsync(IUserMessage voteMessage)
    {
        var citizen = new CitizenGroup(_rolesData.AliveRoles.Values.ToList(), _context.GameRoleOptions);

        var votingResult = await citizen.VoteManyAsync(_context, _token, voteMessage);

        return votingResult;
    }

    private async Task DoNightMovesAsync()
    {
        var exceptRoles = new List<GameRole>(_rolesData.Murders.Values);

        var roles = _rolesData.AliveRoles.Values.ToList();


        //handle specific roles

        if (!_settings.Current.GameSubSettings.IsCustomGame || _settings.Current.RolesInfoSubSettings.MurdersKnowEachOther)
            await ChangeMurdersPermsAsync(_allowWrite, _allowSpeak);

        if (!_settings.Current.GameSubSettings.IsCustomGame || _settings.Current.RolesInfoSubSettings.MurdersVoteTogether)
        {
            var murdersGroup = new MurdersGroup(_rolesData.Murders.Values.Where(m => m.IsAlive).ToList(), _context.GameRoleOptions);

            roles.Add(murdersGroup);
        }
        else
            foreach (var murder in _rolesData.Murders.Values)
                exceptRoles.Remove(murder);


        roles = roles.Except(exceptRoles).ToList();



        var priorityGroups = roles.GroupBy(r => r.Priority).OrderByDescending(g => g.Key);

        foreach (var rolesGrouping in priorityGroups)
        {
            var tasksSingle = new List<Task<Vote>>();
            var tasksGroup = new List<Task<VoteGroup>>();

            foreach (var role in rolesGrouping)
            {
                if (role is RolesGroup rolesGroup)
                    tasksGroup.Add(rolesGroup.VoteManyAsync(_context, _token));
                else
                    tasksSingle.Add(role.VoteAsync(_context, _token));
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
                            $"{vote.VotedRole.Name} [{vote.VotedRole.Player.GetFullName()}] - {(vote.IsSkip ? "Skip" : vote.Option?.GetFullName() ?? "None")}", "Голосование");

                    await vote.VotedRole.SendVotingResults();
                }
            });

            var taskGroup = Task.Run(async () =>
            {
                while (tasksGroup.Count > 0)
                {
                    var task = await Task.WhenAny(tasksGroup);

                    var votingResult = await task;

                    tasksGroup.Remove(task);

                    if (_guildData.SpectatorTextChannel is not null)
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle("Голосование")
                            .AddField("Игрок", string.Join('\n', votingResult.PlayersVote.Values.Select(vote => $"{vote.VotedRole.Name} [{vote.VotedRole.Player.GetFullName()}]")), true)
                            .AddField("Голос", string.Join('\n', votingResult.PlayersVote.Values.Select(vote => $"{(vote.IsSkip ? "Skip" : vote.Option?.GetFullName() ?? "None")}")), true)
                            .AddField("Результат", votingResult.Choice.IsSkip ? "Skip" : votingResult.Choice.Option?.GetFullName() ?? "None")
                            .Build();

                        await _guildData.SpectatorTextChannel.SendMessageAsync(embed: embed);
                    }
                }
            });


            await Task.WhenAll(taskSingle, taskGroup);
        }
    }

    private IReadOnlyList<IGuildUser> GetCorpses()
    {
        var killers = _rolesData.AliveRoles.Values.Where(r => r is IKiller).Cast<IKiller>();
        var healers = _rolesData.AliveRoles.Values.Where(r => r is IHealer).Cast<IHealer>();

        var kills = killers
            .Where(k => k.KilledPlayer is not null)
            .Select(k => k.KilledPlayer!)
            .ToList();

        var heals = healers
            .Where(h => h.HealedPlayer is not null)
            .Select(k => k.HealedPlayer!);


        var corpses = kills
            .Except(heals)
            .Shuffle()
            .ToList();

        return corpses;
    }



    private async Task EjectPlayerAsync(IGuildUser player, bool isKill = true)
    {
        await _guildData.MurderTextChannel.RemovePermissionOverwriteAsync(player);


        _rolesData.AllRoles[player].GameOver();
        _rolesData.AliveRoles.Remove(player);


        await player.RemoveRoleAsync(_guildData.MafiaRole);


        if (_guildData.PlayerRoleIds.TryGetValue(player.Id, out var rolesIds) && rolesIds.Count > 0)
        {
            await player.AddRolesAsync(rolesIds);
        }

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
            {
                await player.RemoveRoleAsync(_guildData.SpectatorRole);
            }
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
        .AddField("Роль", string.Join("\n", _rolesData.Murders.Values.Select(m => m.Name), true))
        .Build();
}