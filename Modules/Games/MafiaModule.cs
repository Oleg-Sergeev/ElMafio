using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.Games.Stats;
using Infrastructure.Data.ViewModels;
using Microsoft.EntityFrameworkCore;
using Modules.Extensions;

namespace Modules.Games
{
    [Group("Мафия")]
    [Alias("маф", "м")]
    public class MafiaModule : GameModule
    {
        private MafiaData? _mafiaData;

        private MafiaSettings _settings = null!;


        private OverwritePermissions _allowWrite;
        private OverwritePermissions _denyWrite;
        private OverwritePermissions _allowSpeak;
        private OverwritePermissions _denySpeak;


        public MafiaModule(Random random, BotContext db) : base(random, db)
        {

        }

        [Group("Настройки")]
        [Alias("н")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("Настройки для мафии включают в себя настройки сервера(используемые роли, каналы и категорию каналов) и настройки самой игры. " +
            "Для подробностей введите команду **Мафия.Настройки.Помощь**")]
        public class Settings : ModuleBase<SocketCommandContext>
        {
            private readonly BotContext _db;

            public Settings(BotContext db)
            {
                _db = db;
            }


            [Command("Параметры")]
            [Alias("парам", "пар", "п")]
            [Summary("Настроить параметры игры")]
            public async Task SetSettingsAsync([Summary("Список параметров: ")][Remainder] MafiaSettingsViewModel mafiaSettings)
            {
                if (mafiaSettings.MafiaKoefficient < 3)
                {
                    await ReplyAsync("Расчетный коэффициент не может быть меньше 3");

                    return;
                }

                var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

                if (mafiaSettings.MafiaKoefficient is not null) settings.MafiaKoefficient = mafiaSettings.MafiaKoefficient.Value;
                if (mafiaSettings.IsRatingGame is not null) settings.IsRatingGame = mafiaSettings.IsRatingGame.Value;
                if (mafiaSettings.RenameUsers is not null) settings.RenameUsers = mafiaSettings.RenameUsers.Value;
                if (mafiaSettings.ReplyMessagesOnError is not null) settings.ReplyMessagesOnError = mafiaSettings.ReplyMessagesOnError.Value;
                if (mafiaSettings.AbortGameWhenError is not null) settings.AbortGameWhenError = mafiaSettings.AbortGameWhenError.Value;
                if (mafiaSettings.SendWelcomeMessage is not null) settings.SendWelcomeMessage = mafiaSettings.SendWelcomeMessage.Value;

                await _db.SaveChangesAsync();

                await ReplyAsync("Параметры успешно установлены");
            }


            [Priority(0)]
            [Command("Каналы")]
            [Summary("Настроить используемые каналы для игры")]
            [Remarks("Для указания ссылки на голосовой канал, используйте следующий шаблон: <#VoiceChannelId>")]
            public async Task SetSettingsAsync(
                [Summary("Главный канал игры, в котором происходит дневное голосование и объявление результатов прошедшей ночи")] ITextChannel generalTextChannel,
                [Summary("Канал для убийц, в котором обсуждаются планы на следующую жертву и непосредственно само голосование")] ITextChannel murderTextChannel,
                [Summary("Голосовой канал для всех игроков, доступен только днем во время обсуждения")] IVoiceChannel generalVoiceChannel,
                [Summary("Голосовой канал для убийц, доступен только ночью")] IVoiceChannel murderVoiceChannel)
                => await SetSettingsAsync(generalTextChannel.Id, murderTextChannel.Id, generalVoiceChannel.Id, murderVoiceChannel.Id);

            [Priority(1)]
            [Command("Каналы")]
            [Summary("Настроить используемые каналы для игры")]
            [Remarks("Чтобы ввести ID категории канала, нажмите по нужной категории правой кнопкой мыши (ПК), или зажмите пальцем канал (Моб.), " +
                     "а затем нажмите кнопку **Скопировать ID**")]
            public async Task SetSettingsAsync(
                [Summary("Главный канал игры, в котором происходит дневное голосование и объявление результатов прошедшей ночи")] ulong generalTextChannelId,
                [Summary("Канал для убийц, в котором обсуждаются планы на следующую жертву и непосредственно само голосование")] ulong murderTextChannelId,
                [Summary("Голосовой канал для всех игроков, доступен только днем во время обсуждения")] ulong generalVoiceChannelId,
                [Summary("Голосовой канал для убийц, доступен только ночью")] ulong murderVoiceChannelId)

            {
                var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

                settings.GeneralTextChannelId = generalTextChannelId;
                settings.MurdersTextChannelId = murderTextChannelId;
                settings.GeneralVoiceChannelId = generalVoiceChannelId;
                settings.MurdersVoiceChannelId = murderVoiceChannelId;

                await _db.SaveChangesAsync();

                await ReplyAsync("Каналы успешно установлены");
            }


            [Priority(0)]
            [Command("Роли")]
            [Summary("Настроить параметры игры")]
            [Remarks("Чтобы указать нужную роль, введите **@** и выберите необходимую роль")]
            public async Task SetSettingsAsync(
                [Summary("Роль, выдываваемая каждому игроку мафии")] IRole mafiaRole,
                [Summary("Роль, выдываваемая игроку, убитому во время игры, чтобы он мог продолжать наблюдать за игрой")] IRole watcherRole)
                => await SetSettingsAsync(mafiaRole.Id, watcherRole.Id);

            [Priority(1)]
            [Command("Роли")]
            [Summary("Настроить параметры игры")]
            [Remarks("Чтобы указать ID роли, выберите роль с помощью **@** и добавьте перед роль обратную косую черту **\\**; " +
                     "либо зайдите в настройки ролей, найдите нужную, нажмите кнопку **Еще** и нажмите **Скопировать ID**")]
            public async Task SetSettingsAsync(
                [Summary("Роль, выдываваемая каждому игроку мафии")] ulong mafiaRoleId,
                [Summary("Роль, выдываваемая игроку, убитому во время игры, чтобы он мог продолжать наблюдать за игрой")] ulong watcherRoleId)
            {
                var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

                settings.MafiaRoleId = mafiaRoleId;
                settings.WatcherRoleId = watcherRoleId;

                await _db.SaveChangesAsync();

                await ReplyAsync("Роли успешно установлены");
            }

            [Priority(1)]
            [Command("Категория")]
            [Summary("Настроить параметры игры")]
            [Remarks("Чтобы ввести ID категории канала, нажмите по нужной категории правой кнопкой мыши (ПК), или зажмите пальцем канал (Моб.), " +
                     "а затем нажмите кнопку **Скопировать ID**")]
            public async Task SetSettingsAsync([Summary("Категория каналов, в которой будут находиться игровые каналы")] ulong categoryChannelId)
            {
                var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

                settings.CategoryChannelId = categoryChannelId;

                await _db.SaveChangesAsync();

                await ReplyAsync("Категория успешно установлена");
            }




            [Command("Текущие")]
            [Alias("тек")]
            [Summary("Показать текущие настройки игры для этого сервера")]
            public async Task ShowSettingsAsync()
            {
                await ReplyAsync("Embed");
            }
        }



        protected override GameModuleData CreateGameData(IGuildUser creator)
            => new("Мафия", 3, creator);



        public override async Task ResetStatAsync(IGuildUser guildUser)
            => await ResetStatAsync<MafiaStats>(guildUser);


        public override async Task ShowStatsAsync()
            => await ShowStatsAsync(Context.User);

        public override async Task ShowStatsAsync(IUser user)
        {
            var userStat = await _db.MafiaStats
                .AsNoTracking()
                .Include(stat => stat.User)
                .FirstOrDefaultAsync(stat => stat.UserId == user.Id);

            if (userStat is null)
            {
                await ReplyAsync("Статистика отсутствует");

                return;
            }

            await ReplyAsync($"Процент побед: {userStat.WinRate.ToPercent()}");
        }

        public override async Task ShowRating()
        {
            var allStats = await _db.MafiaStats
                .AsNoTracking()
                .Where(s => s.GuildId == Context.Guild.Id)
                .OrderByDescending(stat => stat.TotalWinRate)
                .ThenByDescending(stat => stat.WinsCount)
                .Include(stat => stat.User)
                .ToListAsync();


            if (allStats.Count == 0)
            {
                await ReplyAsync("Рейтинг отсутствует");

                return;
            }


            var playersId = allStats
                .Select(s => s.UserId)
                .ToHashSet();

            var players = Context.Guild.Users
                .Where(u => playersId.Contains(u.Id))
                .ToDictionary(u => u.Id);

            var message = "**Рейтинг мафии:**\n";
            for (int i = 0; i < allStats.Count; i++)
                message += $"{i + 1} - {players[allStats[i].UserId].GetFullName()}  **({allStats[i].TotalWinRate * 100} / 100)**\n";


            await ReplyAsync(message);
        }

        public override Task ResetRatingAsync()
            => ResetRatingAsync<MafiaStats>();


        [RequireBotPermission(GuildPermission.AddReactions
            | GuildPermission.ManageChannels
            | GuildPermission.ManageRoles
            | GuildPermission.ManageGuild
            | GuildPermission.MuteMembers
            | GuildPermission.DeafenMembers
            | GuildPermission.MoveMembers)]
        public override async Task StartAsync()
        {
            GameData = GetGameData();

            if (!CanStart(out var msg))
            {
                await ReplyAsync(msg);

                return;
            }

            GameData!.IsPlaying = true;


            await ReplyAsync($"{GameData.Name} начинается!");

            _settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);


            _mafiaData = await PreSetupGuildAsync();

            _mafiaData.AlivePlayers.AddRange(GameData!.Players);

            try
            {
                await SetupGuildAsync();

                await SetupPlayersAsync();

                await SetupRolesAsync();

                await NotifyPlayersAsync();


                await PlayAsync();

                await _mafiaData.GeneralTextChannel.SendMessageAsync("Игра завершена");


                var isMafiaWon = _mafiaData.Murders.Count > 0;

                await FinishAsync(isMafiaWon);


                if (GameData.IsPlaying)
                {
                    await ReplyAsync($"{(isMafiaWon ? "Мафия победила!" : "Мирные жители победили!")} Благодарим за участие!");

                    await ReplyAsync($"Участники и их роли:\n{_mafiaData.PlayerGameRoles}");
                }


                if (GameData.IsPlaying && _settings.IsRatingGame)
                    await SaveStatsAsync();

                await ReplyAsync("Игра завершена");
            }
            catch (GameAbortedException e)
            {
                await AbortAsync();

                await ReplyAsync($"Игра остановлена. Причина: {e.Message}");
            }
            catch (Exception)
            {
                await AbortAsync();

                await ReplyAsync("Игра аварийно прервана");

                throw;
            }
            finally
            {
                if (_settings.GeneralTextChannelId != _mafiaData.GeneralTextChannel.Id)
                    await SetAndSaveSettingsToDbAsync();

                DeleteGameData();
            }
        }


        private void SetCategoryChannel(GuildChannelProperties props) 
            => props.CategoryId = _settings.CategoryChannelId;

        private async Task<MafiaData> PreSetupGuildAsync()
        {
            int messagesToDelete = Context.Guild.CurrentUser.GuildPermissions.ManageMessages ? 500 : 0;

            return new(await Context.Guild.GetTextChannelOrCreateAsync(_settings.GeneralTextChannelId, "мафия-общий", messagesToDelete, SetCategoryChannel),
                   await Context.Guild.GetTextChannelOrCreateAsync(_settings.MurdersTextChannelId, "мафия-убийцы", messagesToDelete, SetCategoryChannel),
                   await Context.Guild.GetVoiceChannelOrCreateAsync(_settings.GeneralVoiceChannelId, "мафия-общий", SetCategoryChannel),
                   await Context.Guild.GetVoiceChannelOrCreateAsync(_settings.MurdersVoiceChannelId, "мафия-убийцы", SetCategoryChannel),
                   await Context.Guild.GetRoleOrCreateAsync(_settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true),
                   await Context.Guild.GetRoleOrCreateAsync(_settings.WatcherRoleId, "Наблюдатель мафии", null, Color.DarkBlue, true, true));
        }


        private async Task SetupGuildAsync()
        {
            await ReplyAsync("Подготавливаем сервер...");

            foreach (var channel in Context.Guild.Channels)
                await channel.AddPermissionOverwriteAsync(_mafiaData!.MafiaRole, OverwritePermissions.DenyAll(channel));


            ConfigureOverwritePermissions();

            await _mafiaData!.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);
            await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);

            await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new(viewChannel: PermValue.Deny));
            await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new(viewChannel: PermValue.Deny));
        }

        private async Task SetupPlayersAsync()
        {
            await ReplyAsync("Собираем досье на игроков...");

            foreach (var player in GameData!.Players)
            {
                if (_settings.SendWelcomeMessage)
                {
                    try
                    {
                        await player.SendMessageAsync("Добро пожаловать в мафию! Скоро я вышлю вам вашу роль и вы начнете играть.");
                    }
                    catch (HttpException e) when (e.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        var msg = $"Не удалось отправить сообщение пользователю {player.GetFullMention()}";

                        await HandleHttpExceptionAsync(msg, e);
                    }
                    catch (Exception)
                    {
                        await AbortAsync();

                        throw;
                    }
                }


                await _mafiaData!.MurderTextChannel.RemovePermissionOverwriteAsync(player);

                _mafiaData.PlayerRoles.Add(player.Id, new List<IRole>());

                _mafiaData.PlayerStats.Add(player.Id, new MafiaStats
                {
                    UserId = player.Id
                });


                var guildPlayer = (SocketGuildUser)player;

                if (Context.Guild.CurrentUser.GuildPermissions.ManageNicknames && _settings.RenameUsers && guildPlayer.Id != Context.Guild.OwnerId && guildPlayer.Nickname is null)
                {
                    try
                    {
                        await guildPlayer.ModifyAsync(props => props.Nickname = $"_{guildPlayer.Username}_");

                        _mafiaData!.OverwrittenNicknames.Add(guildPlayer.Id);
                    }
                    catch (HttpException e) when (e.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        var msg = $"Не удалось назначить ник пользователю {guildPlayer.GetFullMention()}";

                        await HandleHttpExceptionAsync(msg, e);
                    }
                    catch (Exception)
                    {
                        await AbortAsync();

                        throw;
                    }
                }

                foreach (var role in guildPlayer.Roles)
                    if (!role.IsEveryone && role.Id != _mafiaData!.MafiaRole.Id && role.Id != _mafiaData.WatcherRole.Id)
                    {
                        try
                        {
                            await guildPlayer.RemoveRoleAsync(role);

                            _mafiaData.PlayerRoles[player.Id].Add(role);
                        }
                        catch (HttpException e) when (e.HttpCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            var msg = $"Не удалось убрать роль **{role}** у пользователя {guildPlayer.GetFullMention()}";

                            await HandleHttpExceptionAsync(msg, e);
                        }
                        catch (Exception)
                        {
                            await AbortAsync();

                            throw;
                        }
                    }

                await player.AddRoleAsync(_mafiaData!.MafiaRole);
            }
        }

        private async Task SetupRolesAsync()
        {
            await ReplyAsync("Выдаем игрокам роли...");

            GameData!.Players.Shuffle();

            var count = GameData.Players.Count;
            var sheriffIndex = Random.Next(count);

            var doctorIndex = sheriffIndex == count - 1
                ? sheriffIndex - 1 - Random.Next(sheriffIndex)
                : sheriffIndex + 1 + Random.Next(count - sheriffIndex - 1);

            _mafiaData!.Commissioner = GameData.Players[sheriffIndex];
            _mafiaData.Doctor = GameData.Players[doctorIndex];

            var otherPlayers = GameData.Players.Except(new List<IGuildUser>() { _mafiaData.Commissioner, _mafiaData.Doctor }).ToList();

            for (int i = 0; i <= GameData.Players.Count / _settings.MafiaKoefficient; i++)
                _mafiaData.Murders.Add(otherPlayers[i]);

            //if (_mafiaData.Murders.Count >= 3)
            //{
            //    _mafiaData.Don = _mafiaData.Murders[Random.Next(_mafiaData.Murders.Count)];

            //    _mafiaData.HasDon = true;
            //}
        }

        private async Task NotifyPlayersAsync()
        {
            foreach (var player in GameData!.Players)
            {
                var text = "Вы - Мирный житель. Ваша цель - вычислить и изгнать убийц";
                var role = "Мирный житель";

                if (_mafiaData!.Murders.Contains(player))
                {
                    if (true)
                    {
                        role = "Мафия";
                        text = "Ваша роль - Мафия. Ваша цель - убить всех мирных жителей";
                    }
                    //else
                    //{
                    //    role = "Дон";
                    //    text = "Ваша роль - Дон. Ваша цель - вычислить шерифа и с остальными мафиози убить всех мирных жителей";
                    //}
                }
                else
                {
                    if (player == _mafiaData.Doctor)
                    {
                        role = "Доктор";
                        text = "Ваша роль - Доктор. Ваша цель - помочь жителям победить, леча каждую ночь одного из них, или себя";
                    }
                    else if (player == _mafiaData.Commissioner)
                    {
                        role = "Шериф";
                        text = "Ваша роль - Шериф. Ваша цель - помочь жителям победить, узнавая роль определенного жителя каждую ночь";
                    }
                }


                _mafiaData.PlayerGameRoles += $"{player.GetFullName()} - {role}\n";


                try
                {
                    await player.SendMessageAsync(text);
                }
                catch (HttpException e)
                {
                    var msg = $"Не удалось отправить сообщение пользователю {player.GetFullMention()}";

                    await HandleHttpExceptionAsync(msg, e);
                }
                catch (Exception)
                {
                    await AbortAsync();

                    throw;
                }
            }
        }


        private async Task AbortAsync()
        {
            for (int i = _mafiaData!.AlivePlayers.Count - 1; i >= 0; i--)
            {
                var player = _mafiaData.AlivePlayers[i];

                await EjectPlayerAsync(player, false);
            }

            foreach (var player in _mafiaData.KilledPlayers)
            {
                await player.RemoveRoleAsync(_mafiaData.WatcherRole);

            }
        }

        private async Task FinishAsync(bool isMafiaWon)
        {
            for (int i = _mafiaData!.AlivePlayers.Count - 1; i >= 0; i--)
            {
                var player = _mafiaData.AlivePlayers[i];

                if (GameData!.IsPlaying)
                {
                    if (_mafiaData.Murders.Contains(player))
                    {
                        if (isMafiaWon)
                        {
                            _mafiaData.PlayerStats[player.Id].MurderWinsCount++;
                            _mafiaData.PlayerStats[player.Id].WinsCount++;
                        }
                    }
                    else if (!isMafiaWon) _mafiaData.PlayerStats[player.Id].WinsCount++;
                }


                await EjectPlayerAsync(player, false);
            }

            foreach (var player in _mafiaData.KilledPlayers)
            {
                await player.RemoveRoleAsync(_mafiaData.WatcherRole);

                if (GameData!.IsPlaying)
                {
                    if (_mafiaData.Murders.Contains(player))
                    {
                        if (isMafiaWon)
                        {
                            _mafiaData.PlayerStats[player.Id].MurderWinsCount++;
                            _mafiaData.PlayerStats[player.Id].WinsCount++;
                        }
                    }
                    else if (!isMafiaWon) _mafiaData.PlayerStats[player.Id].WinsCount++;
                }
            }

        }

        private async Task PlayAsync()
        {
            await ChangePermissionsGenaralChannelsAsync(_denyWrite, _denySpeak);

            await _mafiaData!.GeneralTextChannel.SendMessageAsync($"Добро пожаловать в мафию! Сейчас ночь, весь город спит, а мафия знакомится в отдельном чате.");

            await _mafiaData.GeneralTextChannel.SendMessageAsync($"Количество мафиози - {_mafiaData.Murders.Count}");

            if (_mafiaData.Murders.Count > 1)
            {
                var meetTime = _mafiaData.Murders.Count * 10;

                await ChangePermissionsMurderChannelsAsync(_allowWrite, _allowSpeak);

                await _mafiaData.MurderTextChannel.SendMessageAsync("Добро пожаловать в мафию! Сейчас ночь, весь город спит, самое время познакомиться с остальными мафиозниками");


                var murdersList = "";
                foreach (var murder in _mafiaData.Murders) murdersList += $"{murder.GetFullName()}\n";

                await _mafiaData.MurderTextChannel.SendMessageAsync($"Список мафиози:\n{murdersList}");

                //if (_mafiaData.Don != null) await _mafiaData.ChannelMurdersText.SendMessageAsync($"Ваш дон:\n{_mafiaData.Don.GetFullName()}");


                await Task.Delay(meetTime * 1000);

                await WaitTimerAsync(meetTime, _mafiaData.GeneralTextChannel, _mafiaData.MurderTextChannel);

                await _mafiaData.MurderTextChannel.SendMessageAsync("Время вышло! Переходите в общий канал и старайтесь не подавать виду, что вы мафиозник.");
            }


            var nightMurderVoteTime = 30;
            var nightInnocentVoteTime = 40 + _mafiaData.Murders.Count * 10;
            var nightTime = 30 + _mafiaData.Murders.Count * 5;
            var dayVoteTime = 30;

            while (GameData!.IsPlaying && _mafiaData.Murders.Count > 0 && _mafiaData.AlivePlayers.Count > 2 * _mafiaData.Murders.Count)
            {
                var dayTime = 10 + (_mafiaData.AlivePlayers.Count * 10);

                await ChangePermissionsMurderChannelsAsync(_denyWrite, _denySpeak);

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"{_mafiaData.MafiaRole.Mention} Доброе утро, жители города! Самое время пообщаться всем вместе.");

                if (!_mafiaData.IsFirstNight)
                {
                    var wasShot = _mafiaData.MurdersMove is not null;
                    var wasKill = wasShot && _mafiaData.MurdersMove != _mafiaData.DoctorMove;

                    if (_mafiaData.Doctor is not null && wasShot && !wasKill)
                        _mafiaData.PlayerStats[_mafiaData.Doctor.Id].DoctorSuccessfullMovesCount++;

                    await _mafiaData.GeneralTextChannel.SendMessageAsync($"Но сначала новости: сегодня утром, в незаправленной постели...");

                    await Task.Delay(2500);

                    await _mafiaData.GeneralTextChannel.SendMessageAsync(
                        $"{(wasKill ? $"Был обнаружен труп {_mafiaData.MurdersMove!.Mention}" : "Никого не оказалось. Все живы.")}");

                    if (wasKill) await EjectPlayerAsync(_mafiaData.MurdersMove!);

                    if (_mafiaData.Murders.Count == 0 || _mafiaData.AlivePlayers.Count <= 2 * _mafiaData.Murders.Count) break;
                }

                if (!GameData.IsPlaying) break;

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"Обсуждайте. ({dayTime}с)");

                await ChangePermissionsGenaralChannelsAsync(_allowWrite, _allowSpeak);

                await WaitTimerAsync(dayTime, _mafiaData.GeneralTextChannel);

                await ChangePermissionsGenaralChannelsAsync(_denyWrite, _denySpeak);

                if (!GameData.IsPlaying) break;

                if (!_mafiaData.IsFirstNight)
                {
                    await _mafiaData.GeneralTextChannel.SendMessageAsync(
                        $"{_mafiaData.MafiaRole.Mention} Время голосовать! Выбирайте жителя, который будет изгнан сегодня. ({dayVoteTime}с)");

                    await Task.Delay(1000);

                    var kickedPlayer = await MakeVotingAsync(_mafiaData.GeneralTextChannel, dayVoteTime, _mafiaData.AlivePlayers);

                    if (kickedPlayer != null)
                    {
                        await _mafiaData.GeneralTextChannel.SendMessageAsync(
                            $"По результатам голосования нас покидает {kickedPlayer.Mention}. Надеемся, что жители сделали правильный выбор...");

                        await EjectPlayerAsync(kickedPlayer);

                        if (_mafiaData.Murders.Count == 0 || _mafiaData.AlivePlayers.Count <= 2 * _mafiaData.Murders.Count) break;
                    }
                    else await _mafiaData.GeneralTextChannel.SendMessageAsync("Вы не смогли прийти к единому решению. Никто не будет изгнан сегодня.");
                }
                else _mafiaData.IsFirstNight = false;

                if (!GameData.IsPlaying) break;

                await Task.Delay(2000);
                await _mafiaData.GeneralTextChannel.SendMessageAsync("Город засыпает...");

                var tasks = new Task[]
                {
                    DoMurdersMove(nightTime, nightMurderVoteTime),
                    DoDoctorMove(nightInnocentVoteTime),
                    DoCommissionerMove(nightInnocentVoteTime)
                };

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    await ReplyAsync($"Tasks - {ex.Message}");
                }
            }
        }


        private async Task DoMurdersMove(int nightTime, int nightMurderVoteTime)
        {
            var extraTime = 0;
            if (_mafiaData!.Murders.Count > 1)
            {
                await ChangePermissionsMurderChannelsAsync(_allowWrite, _allowSpeak);
                await _mafiaData.MurderTextChannel.SendMessageAsync($"Кто же погибнет этой ночью? Обсуждайте ({nightTime}с)");

                await WaitTimerAsync(nightTime, _mafiaData.MurderTextChannel);
            }
            else extraTime += nightTime;

            await _mafiaData.MurderTextChannel.SendMessageAsync(
                $"{_mafiaData.MafiaRole.Mention} Время голосовать! Выбирайте жителя, который будет убит сегодня ({nightMurderVoteTime + extraTime}с)");

            await ChangePermissionsMurderChannelsAsync(_denyWrite, _denySpeak);

            _mafiaData.MurdersMove = await MakeVotingAsync(_mafiaData.MurderTextChannel, nightMurderVoteTime + extraTime, _mafiaData.AlivePlayers);

            await _mafiaData.MurderTextChannel.SendMessageAsync($"Решение сделано. Вы наведаетесь этой ночью к " +
            $"{(_mafiaData.MurdersMove is not null ? _mafiaData.MurdersMove.GetFullName() : "никому")}");

            await _mafiaData.MurderTextChannel.SendMessageAsync("Ожидайте наступления утра");
            await _mafiaData.GeneralTextChannel.SendMessageAsync("Мафия зарядила 1 пулю для своей жертвы.");
        }

        private async Task DoDoctorMove(int nightInnocentVoteTime)
        {
            if (_mafiaData!.Doctor is not null)
            {
                _mafiaData.PlayerStats[_mafiaData.Doctor.Id].DoctorMovesCount++;

                await _mafiaData.Doctor.SendMessageAsync($"Доктор, ваш ход! Решайте, кто сегодня получит жизненно необходимую медицинскую помощь. ({nightInnocentVoteTime}с)");

                var except = new List<IGuildUser>();
                if (_mafiaData.DoctorMove is not null) except.Add(_mafiaData.DoctorMove);

                if (_mafiaData.HasDoctorSelfHealed) except.Add(_mafiaData.Doctor);


                _mafiaData.DoctorMove = await MakeVotingAsync(
                    await _mafiaData.Doctor.GetOrCreateDMChannelAsync(),
                    nightInnocentVoteTime,
                    _mafiaData.AlivePlayers.Except(except).ToList());


                if (_mafiaData.DoctorMove == _mafiaData.Doctor) _mafiaData.HasDoctorSelfHealed = true;

                await _mafiaData.Doctor.SendMessageAsync($"Решение сделано. Этой ночью вы вылечите " +
                   $"{(_mafiaData.DoctorMove is not null ? _mafiaData.DoctorMove.GetFullName() : "никого")}");

                await _mafiaData.Doctor.SendMessageAsync("Ожидайте наступления утра");
            }
            else
            {
                _mafiaData.DoctorMove = null;

                await WaitTimerAsync(nightInnocentVoteTime);
            }

            await _mafiaData.GeneralTextChannel.SendMessageAsync("Доктор выбрал чья жизнь сегодня в безопасности");
        }

        private async Task DoCommissionerMove(int nightInnocentVoteTime)
        {
            if (_mafiaData!.Commissioner is not null)
            {
                _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].CommissionerMovesCount++;

                await _mafiaData.Commissioner.SendMessageAsync($"Ваш ход, шериф! Делайте выбор, к кому наведаться с проверкой сегодня. ({nightInnocentVoteTime}с)");

                var sheriffMove = await MakeVotingAsync(
                    await _mafiaData.Commissioner.GetOrCreateDMChannelAsync(),
                    nightInnocentVoteTime,
                    _mafiaData.AlivePlayers.Except(new List<IGuildUser>() { _mafiaData.Commissioner }).ToList());

                if (sheriffMove != null)
                {
                    await _mafiaData.Commissioner.SendMessageAsync($"{sheriffMove.GetFullName()} является");

                    await Task.Delay(1500);

                    var isMurderFound = _mafiaData.Murders.Contains(sheriffMove);

                    if (isMurderFound) _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].CommissionerSuccessfullMovesCount++;

                    var text = isMurderFound ? "Мафией!" : "Мирным жителем.";

                    await _mafiaData.Commissioner.SendMessageAsync(text);
                }
                else await _mafiaData.Commissioner.SendMessageAsync("Вы не смогли принять решение.");

                await _mafiaData.Commissioner.SendMessageAsync("Ожидайте наступления утра");
            }
            else await WaitTimerAsync(nightInnocentVoteTime);

            await _mafiaData.GeneralTextChannel.SendMessageAsync("Шериф уже взял ордер на обыск квартиры одного из жителей.");
        }


        private async Task ChangePermissionsMurderChannelsAsync(OverwritePermissions textPerms, OverwritePermissions voicePerms)
        {
            foreach (var murder in _mafiaData!.Murders)
            {
                await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(murder, textPerms);
                await _mafiaData.MurderVoiceChannel.AddPermissionOverwriteAsync(murder, voicePerms);

                if (murder.VoiceChannel != null && voicePerms.ViewChannel == PermValue.Deny)
                    await murder.ModifyAsync(props => props.Channel = null);
            }
        }

        private async Task ChangePermissionsGenaralChannelsAsync(OverwritePermissions textPerms, OverwritePermissions voicePerms)
        {
            await _mafiaData!.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.MafiaRole, textPerms);
            await _mafiaData.GeneralVoiceChannel.AddPermissionOverwriteAsync(_mafiaData.MafiaRole, voicePerms);

            foreach (var player in _mafiaData.AlivePlayers)
            {
                if (player.VoiceChannel != null && voicePerms.ViewChannel == PermValue.Deny)
                    await player.ModifyAsync(props => props.Channel = null);
            }
        }


        private async Task<IGuildUser?> MakeVotingAsync(IMessageChannel channel, int voteTime, IList<IGuildUser> playersToEject)
        {
            try
            {
                var guildChannel = channel as IGuildChannel;
                if (guildChannel is not null) await guildChannel.RemovePermissionOverwriteAsync(_mafiaData!.WatcherRole);

                var text = "";
                var emojis = new Emoji[playersToEject.Count + 1];

                for (int i = 0; i < playersToEject.Count; i++)
                {
                    text += $"{(char)(i + 65)} - {playersToEject[i].GetFullName()}\n";

                    var emoji = new Emoji(((char)(i + 65)).ConvertToSmile());

                    emojis[i] = emoji;
                }

                text += $"0 - Пропустить голосование\n";
                emojis[playersToEject.Count] = new Emoji(0.ConvertToSmile());

                var voting = await channel.SendMessageAsync(text);

                await voting.AddReactionsAsync(emojis);

                await WaitTimerAsync(voteTime, channel);

                voting = (IUserMessage)await channel.GetMessageAsync(voting.Id);

                var maxCount1 = 0;
                var maxCount2 = 0;

                int maxIndex = 0;
                int index = 0;
                foreach (var reaction in voting.Reactions.Values)
                {
                    var count = reaction.ReactionCount;

                    if (count > maxCount1)
                    {
                        maxCount2 = maxCount1;
                        maxCount1 = count;

                        maxIndex = index;
                    }
                    else if (count > maxCount2)
                    {
                        maxCount2 = count;
                    }

                    index++;
                }

                IGuildUser? selectedUser =
                    maxCount1 > maxCount2 && maxIndex < playersToEject.Count
                    ? playersToEject[maxIndex]
                    : null;

                if (guildChannel is not null) await guildChannel.AddPermissionOverwriteAsync(_mafiaData!.WatcherRole, _denyWrite);

                return selectedUser;
            }
            catch (Exception ex)
            {
                await ReplyAsync($"MakeVoting: {ex.Message} at {ex.StackTrace}");

                return null;
            }
        }

        private async Task EjectPlayerAsync(IGuildUser player, bool isKill = true)
        {
            await _mafiaData!.MurderTextChannel.RemovePermissionOverwriteAsync(player);


            _mafiaData.AlivePlayers.Remove(player);
            _mafiaData.Murders.Remove(player);

            if (_mafiaData.Commissioner == player) _mafiaData.Commissioner = null;
            else if (_mafiaData.Doctor == player) _mafiaData.Doctor = null;


            await player.RemoveRoleAsync(_mafiaData.MafiaRole);


            if (_mafiaData.PlayerRoles.ContainsKey(player.Id))
                await player.AddRolesAsync(_mafiaData.PlayerRoles[player.Id]);

            if (_mafiaData.OverwrittenNicknames.Contains(player.Id))
            {
                await player.ModifyAsync(props => props.Nickname = null);

                _mafiaData.OverwrittenNicknames.Remove(player.Id);
            }


            if (isKill)
            {
                await player.AddRoleAsync(_mafiaData.WatcherRole);

                _mafiaData.KilledPlayers.Add(player);
            }
        }

        private static async Task WaitTimerAsync(int seconds, params IMessageChannel[] channels)
        {
            if (seconds >= 20)
            {
                if (seconds >= 40)
                {
                    seconds /= 2;
                    await Task.Delay(seconds * 1000);

                    foreach (var channel in channels)
                        await channel.SendMessageAsync($"Осталось {seconds}с!");

                    await Task.Delay((seconds - 10) * 1000);
                }
                else await Task.Delay((seconds - 10) * 1000);

                seconds -= seconds - 10;

                foreach (var channel in channels)
                    await channel.SendMessageAsync($"Осталось {seconds}с!");
            }

            await Task.Delay(seconds * 1000);
        }


        private async Task HandleHttpExceptionAsync(string message, HttpException e)
        {
            if (_settings.ReplyMessagesOnError)
                await ReplyAsync(message);

            if (_settings.AbortGameWhenError)
                throw new GameAbortedException(message, e);
        }


        private void ConfigureOverwritePermissions()
        {
            _allowWrite = OverwritePermissions.DenyAll(_mafiaData!.GeneralTextChannel).Modify(
               viewChannel: PermValue.Allow,
               readMessageHistory: PermValue.Allow,
               sendMessages: PermValue.Allow);

            _denyWrite = OverwritePermissions.DenyAll(_mafiaData.GeneralTextChannel).Modify(
                viewChannel: PermValue.Allow,
                readMessageHistory: PermValue.Allow);

            _allowSpeak = OverwritePermissions.DenyAll(_mafiaData.GeneralVoiceChannel).Modify(
                viewChannel: PermValue.Allow,
                connect: PermValue.Allow,
                useVoiceActivation: PermValue.Allow,
                speak: PermValue.Allow
                );

            _denySpeak = OverwritePermissions.DenyAll(_mafiaData.GeneralVoiceChannel);
        }


        private async Task SaveStatsAsync()
        {
            if (_mafiaData is null)
                throw new NullReferenceException($"{nameof(_mafiaData)} is null");

            var playersId = new HashSet<ulong>(GameData!.Players.Select(p => p.Id));

            await AddNewUsersAsync(playersId);

            var playersStat = await _db.MafiaStats
                      .AsTracking()
                      .Where(stat => playersId.Contains(stat.UserId) && stat.GuildId == Context.Guild.Id)
                      .ToListAsync();

            var newPlayersId = playersId
                .Except(playersStat.Select(s => s.UserId))
                .ToList();

            if (newPlayersId.Count > 0)
            {
                var newPlayersStats = newPlayersId.Select(id => new MafiaStats
                {
                    UserId = id,
                    GuildId = Context.Guild.Id
                })
                .ToList();

                await _db.MafiaStats.AddRangeAsync(newPlayersStats);

                playersStat.AddRange(newPlayersStats);
            }

            foreach (var playerStat in playersStat)
            {
                var gameStat = _mafiaData.PlayerStats[playerStat.UserId];

                playerStat.GamesCount++;
                playerStat.WinsCount += gameStat.WinsCount;

                playerStat.MurderGamesCount += gameStat.MurderGamesCount;
                playerStat.MurderWinsCount += gameStat.MurderWinsCount;

                playerStat.DoctorMovesCount += gameStat.DoctorMovesCount;
                playerStat.DoctorSuccessfullMovesCount += gameStat.DoctorSuccessfullMovesCount;

                playerStat.CommissionerMovesCount += gameStat.CommissionerMovesCount;
                playerStat.CommissionerSuccessfullMovesCount += gameStat.CommissionerSuccessfullMovesCount;

                playerStat.ExtraScores += gameStat.ExtraScores;
            }


            await _db.SaveChangesAsync();
        }


        private async Task SetAndSaveSettingsToDbAsync()
        {
            _settings.GeneralTextChannelId = _mafiaData!.GeneralTextChannel.Id;
            _settings.GeneralVoiceChannelId = _mafiaData.GeneralVoiceChannel.Id;
            _settings.MurdersTextChannelId = _mafiaData.MurderTextChannel.Id;
            _settings.MurdersVoiceChannelId = _mafiaData.MurderVoiceChannel.Id;
            _settings.MafiaRoleId = _mafiaData.MafiaRole.Id;
            _settings.WatcherRoleId = _mafiaData.WatcherRole.Id;

            await _db.SaveChangesAsync();
        }


        private class MafiaData
        {
            public Dictionary<ulong, MafiaStats> PlayerStats { get; }

            public Dictionary<ulong, ICollection<IRole>> PlayerRoles { get; }

            public List<ulong> OverwrittenNicknames { get; }


            public List<IGuildUser> KilledPlayers { get; }

            public List<IGuildUser> AlivePlayers { get; }


            public List<IGuildUser> Murders { get; }

            public IGuildUser? Doctor { get; set; }
            public IGuildUser? Commissioner { get; set; }


            public IGuildUser? DoctorMove { get; set; }
            public IGuildUser? MurdersMove { get; set; }


            public ITextChannel GeneralTextChannel { get; }
            public ITextChannel MurderTextChannel { get; }

            public IVoiceChannel GeneralVoiceChannel { get; }
            public IVoiceChannel MurderVoiceChannel { get; }


            public IRole MafiaRole { get; }
            public IRole WatcherRole { get; }


            public bool IsFirstNight { get; set; }

            public bool HasDoctorSelfHealed { get; set; }

            public string PlayerGameRoles { get; set; }


            public MafiaData(ITextChannel generalTextChannel,
                             ITextChannel murderTextChannel,
                             IVoiceChannel generalVoiceChannel,
                             IVoiceChannel murderVoiceChannel,
                             IRole mafiaRole,
                             IRole watcherRole)
            {
                PlayerStats = new();

                PlayerRoles = new();

                OverwrittenNicknames = new();


                KilledPlayers = new();

                AlivePlayers = new();


                Murders = new();


                IsFirstNight = true;

                PlayerGameRoles = "";


                GeneralTextChannel = generalTextChannel;
                MurderTextChannel = murderTextChannel;
                GeneralVoiceChannel = generalVoiceChannel;
                MurderVoiceChannel = murderVoiceChannel;
                MafiaRole = mafiaRole;
                WatcherRole = watcherRole;
            }
        }
    }
}
