using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Data;
using Core.Exceptions;
using Core.Extensions;
using Discord;
using Discord.Net;
using Fergun.Interactive;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Microsoft.VisualBasic;
using Modules.Games.Mafia.Common;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.Services;
using static System.Collections.Specialized.BitVector32;

namespace Modules.Games.Mafia;

public class MafiaGame
{
    private readonly IMafiaSetupService _mafiaService;

    private readonly MafiaContext _context;

    private readonly MafiaSettingsTemplate _template;
    private readonly MafiaGuildData _guildData;
    private readonly MafiaRolesData _rolesData;

    private readonly InteractiveService _interactive;

    private readonly MafiaChronology _chronology;

    private readonly OverwritePermissions _denyView;
    private readonly OverwritePermissions _denyWrite;
    private readonly OverwritePermissions _allowWrite;
    private readonly OverwritePermissions? _allowSpeak;

    private readonly CancellationToken _token;

    private bool _isZeroDay;


    public MafiaGame(MafiaContext context, IMafiaSetupService mafiaService)
    {
        _context = context;

        _template = context.SettingsTemplate;
        _guildData = context.GuildData;
        _rolesData = context.RolesData;

        _interactive = context.Interactive;

        _chronology = new();

        _denyView = MafiaHelper.DenyView;
        _denyWrite = MafiaHelper.DenyWrite;
        _allowWrite = MafiaHelper.AllowWrite;

        if (_guildData.GeneralVoiceChannel is not null)
            _allowSpeak = MafiaHelper.AllowSpeak;

        _token = context.MafiaData.TokenSource.Token;

        _isZeroDay = true;
        _mafiaService = mafiaService;
    }


    public async Task<MafiaInfo> RunAsync()
    {
        try
        {
            try
            {
                await _context.CommandContext.Channel.SendEmbedAsync("Подготовка к игре...", EmbedStyle.Waiting);

                await _mafiaService.SendWelcomeMessageAsync(_context);

                _mafiaService.SetupRoles(_context);

                var tasks = new List<Task>
                {
                    _mafiaService.SendRolesInfoAsync(_context),
                    _mafiaService.SetupGuildAsync(_context),
                    _mafiaService.SetupUsersAsync(_context)
                };

                Task.WaitAll(tasks.ToArray());

                await Task.Delay(3000);
            }
            catch (GameSetupAbortedException)
            {
                throw;
            }
            catch (AggregateException ae)
            {
                var e = ae.Flatten();
                throw new GameSetupAbortedException(e.Message, e);
            }
            catch (Exception e)
            {
                throw new GameSetupAbortedException($"Ошибка во время подготовки игры\n{e}", e);
            }

            var msg = $"{_context.MafiaData.Name} успешно запущена";

            await _context.CommandContext.Channel.SendEmbedAsync(msg, EmbedStyle.Successfull);
            await _guildData.GeneralTextChannel.SendEmbedAsync(msg, EmbedStyle.Successfull);


            if (_template.GameSubSettings.PreGameMessage is not null)
            {
                await _guildData.GeneralTextChannel.SendEmbedAsync(_template.GameSubSettings.PreGameMessage, "Сообщение перед игрой");

                await Task.Delay(5000);
            }


            await _guildData.GeneralTextChannel.SendMessageAsync(embed: GenerateRolesAmountEmbed());

            await Task.Delay(5000);


            if (_rolesData.Murders.Count > 1 && _template.RolesExtraInfoSubSettings.MurdersKnowEachOther)
            {
                var meetTime = _rolesData.Murders.Count * 10;

                await _guildData.GeneralTextChannel.SendEmbedAsync($"Пока город спит, мафия знакомятся друг с другом. Через {meetTime}с город проснется", EmbedStyle.Waiting);

                await IntroduceMurdersAsync(meetTime);
            }


            await _guildData.GeneralTextChannel.SendEmbedAsync($"Приятной игры", "Мафия");

            await Task.Delay(5000);

            Winner? winner;

            var lastWordNightCount = _template.GameSubSettings.LastWordNightCount + 1;

            if (_guildData.SpectatorTextChannel is not null)
            {
                var playerRoles = string.Join("\n", _rolesData.AllRoles.Values.Select(r => $"{r.Name} - {r.Player.GetFullName()}"));

                _chronology.AddAction(playerRoles);

                await _guildData.SpectatorTextChannel.SendEmbedAsync(playerRoles, "Роли игроков");
            }

            while (true)
            {
                winner = await LoopAsync(lastWordNightCount--);

                if (winner is not null)
                    break;
            }

            await _guildData.GeneralTextChannel.SendEmbedAsync("Игра завершена", "Мафия");

            await Task.Delay(3000);

            return new(winner, _chronology);
        }
        catch (OperationCanceledException)
        {
            return new(Winner.None, _chronology);
        }
        finally
        {
            await ReturnPlayersDataAsync();
        }
    }



    private async Task<Winner?> LoopAsync(int lastWordNightCount)
    {
        _chronology.NextDay();

        if (_guildData.SpectatorTextChannel is not null)
            await _guildData.SpectatorTextChannel.SendEmbedAsync("Утро", $"День {_chronology.CurrentDay}");


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

                if (_template.RolesExtraInfoSubSettings.MurdersKnowEachOther)
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

                var action = _chronology.AddAction("Труп", _rolesData.AllRoles[corpses[i]]);

                if (_guildData.SpectatorTextChannel is not null)
                    await _guildData.SpectatorTextChannel.SendEmbedAsync(action, "Утро");
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
            if (_guildData.SpectatorTextChannel is not null)
                await _guildData.SpectatorTextChannel.SendEmbedAsync($"Голосование", $"День {_chronology.CurrentDay}");

            var citizenVotingResult = await DoCitizenVotingAsync();

            if (citizenVotingResult.Choice.Option is not null)
            {
                var role = _rolesData.AliveRoles[citizenVotingResult.Choice.Option];

                string action;

                if (!fooledPlayerIds.Contains(role.Player.Id))
                {
                    await EjectPlayerAsync(role.Player);

                    action = _chronology.AddAction("Труп", role);
                }
                else
                {
                    await _guildData.GeneralTextChannel.SendEmbedAsync($"{role.Player.Mention} не покидает игру из-за наличия алиби", EmbedStyle.Warning);

                    action = _chronology.AddAction("Использование алиби", role);
                }

                if (_guildData.SpectatorTextChannel is not null)
                    await _guildData.SpectatorTextChannel.SendEmbedAsync(action, "Дневное голосование");
            }
            else
            {
                if (citizenVotingResult.Choice.IsSkip)
                    _chronology.AddAction("Пропуск", citizenVotingResult.Choice.VotedRole);
                else
                    _chronology.AddAction("Не удалось выбрать", citizenVotingResult.Choice.VotedRole);
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
        if (_guildData.SpectatorTextChannel is not null)
            await _guildData.SpectatorTextChannel.SendEmbedAsync("Наступила ночь", $"День {_chronology.CurrentDay}");

        var exceptRoles = new List<GameRole>(_rolesData.Murders.Values.Where(m => m is not Don));

        var roles = _rolesData.AllRoles.Values.ToList();


        //handle specific roles

        if (!_template.GameSubSettings.IsCustomGame || _template.RolesExtraInfoSubSettings.MurdersKnowEachOther)
            await ChangeMurdersPermsAsync(_allowWrite, _allowSpeak);

        if (!_template.GameSubSettings.IsCustomGame || _template.RolesExtraInfoSubSettings.MurdersVoteTogether)
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

                    if (!vote.VotedRole.IsAlive)
                        continue;

                    var action = _chronology.AddAction($"Голосование - {(vote.IsSkip ? "Пропуск" : vote.Option?.GetFullName() ?? "Нет данных")}", vote.VotedRole);

                    if (_guildData.SpectatorTextChannel is not null)
                        await _guildData.SpectatorTextChannel.SendEmbedAsync(action, "Ночь");
                }
            });

            var taskGroup = Task.Run(async () =>
            {
                while (tasksGroup.Count > 0)
                {
                    var task = await Task.WhenAny(tasksGroup);

                    var voteGroup = await task;

                    var choice = voteGroup.Choice;

                    tasksGroup.Remove(task);

                    if (!voteGroup.Choice.VotedRole.IsAlive)
                        continue;

                    _chronology.AddAction($"Групповое голосование - {(choice.IsSkip ? "Пропуск" : choice.Option?.GetFullName() ?? "Нет данных")}", choice.VotedRole);

                    if (_guildData.SpectatorTextChannel is not null)
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle("Ночь")
                            .WithDescription($"Голосование [{voteGroup.Choice.VotedRole}]")
                            .WithInformationMessage()
                            .AddField("Игрок", string.Join('\n', voteGroup.PlayersVote.Values.Select(vote => $"{vote.VotedRole} [{vote.VotedRole.Player.GetFullName()}]")), true)
                            .AddField("Голос", string.Join('\n', voteGroup.PlayersVote.Values.Select(vote => $"{(vote.IsSkip ? "Пропуск" : vote.Option?.GetFullName() ?? "Нет данных")}")), true)
                            .AddField("Результат", voteGroup.Choice.IsSkip ? "Пропуск" : voteGroup.Choice.Option?.GetFullName() ?? "Нет данных")
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
        IEnumerable<IKiller> killers;
        IGuildUser? murdersKill = null;

        if (!_template.GameSubSettings.IsCustomGame || _template.RolesExtraInfoSubSettings.MurdersKnowEachOther)
            killers = _rolesData.AliveRoles.Values.Where(r => r is IKiller and not Maniac).Cast<IKiller>();
        else
        {
            killers = _rolesData.AliveRoles.Values.Where(r => r is IKiller and not Maniac and not Murder).Cast<IKiller>();

            murdersKill = GetMurdersKill();
        }

        var healers = _rolesData.AliveRoles.Values.Where(r => r is IHealer).Cast<IHealer>();

        var kills = killers
            .Where(k => k.KilledPlayer is not null)
            .Select(k => k.KilledPlayer!)
            .Distinct();

        if (murdersKill is not null)
            kills = kills.Append(murdersKill);


        var innocentsKill = GetInnocentsKill();

        if (innocentsKill is not null)
            kills = kills.Append(innocentsKill);

        var heals = healers
            .Where(h => h.HealedPlayer is not null)
            .Select(k => k.HealedPlayer!)
            .Distinct();


        var corpses = kills
            .Except(heals)
            .Distinct()
            .ToList();

        var isRating = !_template.GameSubSettings.IsCustomGame && _template.GameSubSettings.IsRatingGame;

        foreach (var healer in healers.Where(h => !h.IsSkip))
        {
            if (isRating)
                healer.MovesCount++;

            if (healer.HealedPlayer is not null && kills.Any(k => k.Id == healer.HealedPlayer.Id))
            {
                _chronology.AddAction($"Игрок {healer.HealedPlayer.GetFullMention()} спасен", _rolesData.AliveRoles[((GameRole)healer).Player]);

                if (isRating)
                    healer.HealsCount++;
            }
        }

        foreach (var killer in killers.Where(k => !k.IsSkip))
            if (killer.KilledPlayer is not null && corpses.Any(k => k.Id == killer.KilledPlayer.Id))
            {
                _chronology.AddAction($"Игрок {killer.KilledPlayer.GetFullMention()} убит", _rolesData.AliveRoles[((GameRole)killer).Player]);
            }


        var maniacKills = _rolesData.Maniacs.Values
            .Where(m => m.IsAlive && m.KilledPlayer is not null)
            .Select(m => m.KilledPlayer!);


        foreach (var maniac in _rolesData.Maniacs.Values.Where(k => !k.IsSkip))
            if (maniac.KilledPlayer is not null && maniacKills.Any(k => k.Id == maniac.KilledPlayer.Id))
            {
                _chronology.AddAction($"Игрок {maniac.KilledPlayer.GetFullMention()} убит", _rolesData.Maniacs[maniac.Player]);
            }

        foreach (var hooker in _rolesData.Hookers.Values.Where(k => !k.IsSkip))
            if (hooker.HealedPlayer is not null && maniacKills.Any(k => k.Id == hooker.HealedPlayer.Id))
            {
                _chronology.AddAction($"Игрок {hooker.HealedPlayer.GetFullMention()} спасен от маньяка", _rolesData.Hookers[hooker.Player]);
            }

        maniacKills = maniacKills
            .Except(_rolesData.Hookers.Values
                .Where(h => h.IsAlive && h.HealedPlayer is not null)
                .Select(h => h.HealedPlayer!));

        corpses.AddRange(maniacKills);


        var reveals = new List<IGuildUser>();

        foreach (var hooker in _rolesData.Hookers.Values)
        {
            if (!hooker.IsAlive || hooker.HealedPlayer is null)
                continue;


            if (corpses.Contains(hooker.Player))
                corpses.Add(hooker.HealedPlayer);

            if (_rolesData.Maniacs.ContainsKey(hooker.HealedPlayer))
            {
                reveals.Add(hooker.HealedPlayer);

                _chronology.AddAction("Роль маньяка раскрыта", _rolesData.Maniacs[hooker.HealedPlayer]);
            }
        }


        revealedManiacs = reveals;

        return corpses
            .Distinct()
            .Shuffle()
            .ToList();
    }

    private IGuildUser? GetInnocentsKill()
    {
        if (!_template.GameSubSettings.IsCustomGame || !_template.RolesExtraInfoSubSettings.CanInnocentsKillAtNight)
            return null;

        var innocents = _rolesData.Innocents.Values.Where(i => i.IsAlive && i.GetType() == typeof(Innocent));

        if (!innocents.Any())
            return null;

        var playersVotes = innocents.ToDictionary(i => i.Player, i => new Vote(i, i.LastMove, i.IsSkip));

        var voteGroup = new VoteGroup(_rolesData.GroupRoles[nameof(CitizenGroup)], playersVotes, _template.RolesExtraInfoSubSettings.InnocentsMustVoteForOnePlayer);

        return voteGroup.Choice.Option;
    }

    private IGuildUser? GetMurdersKill()
    {
        if (!_template.GameSubSettings.IsCustomGame || !_template.RolesExtraInfoSubSettings.MurdersKnowEachOther)
            return null;

        var murders = _rolesData.Murders.Values.Where(i => i.IsAlive);

        if (!murders.Any())
            return null;

        var playersVotes = murders.ToDictionary(i => i.Player, i => new Vote(i, i.LastMove, i.IsSkip));

        var voteGroup = new VoteGroup(_rolesData.GroupRoles[nameof(MurdersGroup)], playersVotes, _template.RolesExtraInfoSubSettings.MurdersMustVoteForOnePlayer);

        return voteGroup.Choice.Option;
    }

    private async Task<string> SendLastWordMessageAsync(IGuildUser player)
    {
        var dmChannel = await player.CreateDMChannelAsync();

        var msg = await dmChannel.SendMessageAsync(embed: EmbedHelper.CreateEmbed($"{player.Username}, у вас есть 30с для последнего слова, воспользуйтесь этим временем с умом." +
            $"\n**Напишите здесь сообщение, которое увидят все игроки Мафии**"));

        var result = await _interactive.NextMessageAsync(m => m.Channel.Id == msg.Channel.Id && m.Author.Id == player.Id,
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
        if (_rolesData.AllRoles.ContainsKey(player))
        {
            await _context.CommandContext.Channel.SendEmbedAsync($"Игрок {player.GetFullMention()} не найден", EmbedStyle.Error);

            return;
        }

        if (_rolesData.AliveRoles.ContainsKey(player))
        {
            Console.WriteLine("********************\n************************KICK MURDER******************************\n****************************");

            try
            {
                await _guildData.MurderTextChannel.RemovePermissionOverwriteAsync(player);

                if (_guildData.MurderVoiceChannel is not null)
                    await _guildData.MurderVoiceChannel.RemovePermissionOverwriteAsync(player);
            } 
            catch (Exception e)
            {
                await _context.CommandContext.Channel
                    .SendEmbedAsync($"Произошла ошибка: {e.Message}.  Не удалось снять переопределения с мафиози {player.GetFullMention()}", EmbedStyle.Error);
            }
        }

        _rolesData.AllRoles[player].GameOver();
        _rolesData.AliveRoles.Remove(player);


        try
        {
            await player.RemoveRoleAsync(_guildData.MafiaRole);
        }
        catch (Exception e)
        {
            await _context.CommandContext.Channel
                .SendEmbedAsync($"Произошла ошибка: {e.Message}. Не удалось снять роль `{_guildData.MafiaRole.Mention}` с игрока {player.GetFullMention()}", EmbedStyle.Error);
        }

        if (_guildData.PlayerRoleIds.TryGetValue(player.Id, out var rolesIds) && rolesIds.Count > 0)
            try
            {
                await player.AddRolesAsync(rolesIds);
            }
            catch (Exception e)
            {
                await _context.CommandContext.Channel
                    .SendEmbedAsync($"Произошла ошибка: {e.Message}. Не удалось вернуть роли игроку {player.GetFullMention()}", EmbedStyle.Error);
            }

        if (_guildData.OverwrittenNicknames.Contains(player.Id))
        {
            try
            {
                await player.ModifyAsync(props => props.Nickname = null);

                _guildData.OverwrittenNicknames.Remove(player.Id);
            }
            catch (Exception e)
            {
                await _context.CommandContext.Channel
                    .SendEmbedAsync($"Произошла ошибка: {e.Message}. Не удалось восстановить изначальный ник игроку {player.GetFullMention()}", EmbedStyle.Error);
            }
        }


        if (isKill)
        {
            _guildData.KilledPlayers.Add(player);

            if (_guildData.SpectatorTextChannel is not null && _guildData.SpectatorRole is not null)
                try
                {
                    await player.AddRoleAsync(_guildData.SpectatorRole);
                }
                catch (Exception e)
                {
                    await _context.CommandContext.Channel
                        .SendEmbedAsync($"Произошла ошибка: {e.Message}. Не удалось добавить роль `{_guildData.SpectatorRole.Mention}` игроку {player.GetFullMention()}", EmbedStyle.Error);
                }
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
        var gameSettings = _template.GameSubSettings;


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




        Winner GetCitizen() => new(_rolesData.GroupRoles[nameof(CitizenGroup)], _rolesData.Innocents.Keys.Select(p => p.Id).ToList());
        Winner GetMurders() => new(_rolesData.GroupRoles[nameof(MurdersGroup)], _rolesData.Murders.Keys.Select(p => p.Id).ToList());
    }


    private Embed GenerateRolesAmountEmbed()
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