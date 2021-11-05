using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Infrastructure.Data;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Modules.Extensions;

namespace Modules.Games
{
    [Group("РусскаяРулетка")]
    [Alias("рулетка", "р")]
    public class RussianRoulleteModule : GameModule
    {
        public RussianRoulleteModule(Random random, BotContext db) : base(random, db)
        {
        }


        // Maybe add complex adding
        [Group("Смайлы")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public class SetSmile : ModuleBase<SocketCommandContext>
        {
            private readonly BotContext _db;

            public SetSmile(BotContext db)
            {
                _db = db;
            }


            [Command("удалить")]
            public async Task RemoveCustomSmilesAsync()
            {
                await RemoveCustomSmileKilled();

                await RemoveCustomSmileSurvived();
            }

            [Command("удалитьубил")]
            public async Task RemoveCustomSmileKilled()
            {
                var settings = await GetSettingsAsync<RussianRouletteSettings>(_db, Context.Guild.Id);

                settings.CustomSmileKilled = null;

                await _db.SaveChangesAsync();
            }

            [Command("удалитьвыжил")]
            public async Task RemoveCustomSmileSurvived()
            {
                var settings = await GetSettingsAsync<RussianRouletteSettings>(_db, Context.Guild.Id);

                settings.CustomSmileSurvived = null;

                await _db.SaveChangesAsync();
            }



            [Priority(0)]
            [Command("убил")]
            public async Task SetSmileKilledAsync(Emoji emoji)
            {
                var settings = await GetSettingsAsync<RussianRouletteSettings>(_db, Context.Guild.Id);

                settings.UnicodeSmileKilled = emoji.ToString();

                await _db.SaveChangesAsync();


                var msg = await ReplyAsync($"Смайл {emoji} успешно настроен");

                await msg.AddReactionAsync(emoji);
            }

            [Priority(1)]
            [Command("убил")]
            public async Task SetSmileKilledAsync(Emote emote)
            {
                var settings = await GetSettingsAsync<RussianRouletteSettings>(_db, Context.Guild.Id);

                settings.CustomSmileKilled = emote.ToString();

                await _db.SaveChangesAsync();


                var msg = await ReplyAsync($"Смайл {emote} успешно настроен");

                await msg.AddReactionAsync(emote);
            }



            [Priority(0)]
            [Command("выжил")]
            public async Task SetSmileSuvivedAsync(Emoji emoji)
            {
                var settings = await GetSettingsAsync<RussianRouletteSettings>(_db, Context.Guild.Id);

                settings.UnicodeSmileSurvived = emoji.ToString();

                await _db.SaveChangesAsync();


                var msg = await ReplyAsync($"Смайл {emoji} успешно настроен");

                await msg.AddReactionAsync(emoji);
            }

            [Priority(1)]
            [Command("выжил")]
            public async Task SetSmileSuvivedAsync(Emote emote)
            {
                var settings = await GetSettingsAsync<RussianRouletteSettings>(_db, Context.Guild.Id);

                settings.CustomSmileSurvived = emote.ToString();

                await _db.SaveChangesAsync();


                var msg = await ReplyAsync($"Смайл {emote} успешно настроен");

                await msg.AddReactionAsync(emote);
            }
        }


        protected override GameModuleData CreateGameData(IGuildUser creator)
            => new("Русская рулетка", 2, creator);


        public override async Task ResetStatAsync(IGuildUser guildUser)
            => await ResetStatAsync<RussianRouletteStats>(guildUser);

        public override async Task ShowStatsAsync()
            => await ShowStatsAsync(Context.User);

        public override async Task ShowStatsAsync(IUser user)
        {
            var userStat = await _db.RussianRouletteStats
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
            var allStats = await _db.RussianRouletteStats
                .AsNoTracking()
                .Where(s => s.GuildId == Context.Guild.Id)
                .OrderByDescending(stat => stat.WinRate)
                .ThenByDescending(stat => stat.WinsCount)
                .Include(stat => stat.User)
                .ToListAsync();

            var message = "**Рейтинг русской рулетки:**\n";


            for (int i = 0; i < allStats.Count; i++)
            {
                // maybe user left guild
                var bugDebugShit = Context.Guild.Users.FirstOrDefault(u => u.Id == allStats[i].User.Id);



                message += $"{i + 1} - {bugDebugShit!.GetFullName()}  **({allStats[i].WinRate.ToPercent()})**\n";
            }


            await ReplyAsync(message);
        }

        public override Task ResetRatingAsync()
            => ResetRatingAsync<RussianRouletteStats>();

        [RequireBotPermission(GuildPermission.AddReactions,
            ErrorMessage = "У меня нет права добавлять реакции к сообщениям. Пожалуйста, добавьте это право, или используйте команду **стартбез**")]
        public override async Task StartAsync() => await StartGameAsync(true);

        [Command("стартбез")]
        [Summary("Запустить игру без добавления реакций к сообщениям")]
        public async Task StartWithoutReactionsAsync() => await StartGameAsync(false);


        private async Task StartGameAsync(bool canAddReactions)
        {
            GameData = GetGameData();

            if (!CanStart(out var msg))
            {
                await ReplyAsync(msg);

                return;
            }

            GameData!.IsPlaying = true;

            var playersId = new HashSet<ulong>(GameData.Players.Select(p => p.Id));


            await ReplyAsync($"{GameData.Name} начинается!");

            var delay = Task.Delay(3000);

            var rrData = await CreateRussianRoulleteDataAsync();

            await delay;

            var winner = await PlayAsync(rrData, canAddReactions);


            if (GameData.IsPlaying && winner is not null)
            {
                await Task.Delay(2000);


                await ReplyAsync($"И абсолютным чемпионом русской рулетки становится {winner.Mention}! Поздравляем!");


                delay = Task.Delay(1000);

                await AddNewUsersAsync(playersId);

                await delay;

                var updateStatsTask = UpdatePlayersStatAsync(winner, playersId);


                await ReplyAsync("Игра завершена!");


                await updateStatsTask;
            }


            DeleteGameData();
        }

        private async Task<IGuildUser?> PlayAsync(RussianRoulleteData rrData, bool canAddReactions)
        {
            await ReplyAsync("Крутим барабан...");


            var delay = Task.Delay(3000);

            GameData!.Players.Shuffle();

            await delay;


            var alivePlayers = new List<IGuildUser>(GameData.Players);

            var hasCustomSmileKilled = rrData.CustomEmoteKilled is not null;
            var hasCustomSmileSurvived = rrData.CustomEmoteSurvived is not null;

            while (alivePlayers.Count > 1 && GameData.IsPlaying)
            {
                for (int drum = 1, i = alivePlayers.Count; drum <= 6 && GameData.IsPlaying; drum++)
                {
                    if (--i < 0) i = alivePlayers.Count - 1;


                    await ReplyAsync($"На очереди {alivePlayers[i].Mention}");

                    await Task.Delay(3000);

                    if (!GameData.IsPlaying) return null;


                    bool hasKilled = Random.NextDouble() < (double)drum / 6;

                    if (hasKilled)
                    {
                        var messages = new string[4]
                        {
                            "ловит пулю своим стальным лбом",
                            "получает маслину",
                            "просто сдыхает",
                            "героически останавливает пулю ценой своей никчемной жизни"
                        };


                        var msg = await ReplyAsync($"{alivePlayers[i].Mention} жмет на курок и {messages[Random.Next(messages.Length)]}");


                        if (canAddReactions)
                        {
                            if (hasCustomSmileKilled && Context.Guild.Emotes.Contains(rrData.CustomEmoteKilled))
                                await msg.AddReactionAsync(rrData.CustomEmoteKilled);
                            else
                                await msg.AddReactionAsync(rrData.UnicodeEmojiKilled);
                        }



                        alivePlayers.RemoveAt(i);

                        if (alivePlayers.Count < 2) break;

                        alivePlayers.Shuffle();


                        await ReplyAsync($"Не будем унывать, в нашей беспроигрышной лотерее осталось еще {alivePlayers.Count} счастливчиков!");

                        await Task.Delay(2000);

                        await ReplyAsync("Крутим барабан...");

                        await Task.Delay(4000);

                        break;
                    }
                    else
                    {
                        var messages = new string[4]
                        {
                            "посылает нахер смерть",
                            "получает шанс пожить еще пару минут",
                            "спокойно передает револьвер следующему участнику",
                            "жалеет, что остался в живых"
                        };

                        var msg = await ReplyAsync($"{alivePlayers[i].Mention} жмет на курок и {messages[Random.Next(messages.Length)]}");


                        if (canAddReactions)
                        {
                            if (hasCustomSmileSurvived && Context.Guild.Emotes.Contains(rrData.CustomEmoteSurvived))
                                await msg.AddReactionAsync(rrData.CustomEmoteSurvived);
                            else
                                await msg.AddReactionAsync(rrData.UnicodeEmojiSurvived);
                        }



                        await ReplyAsync("А мы идем дальше");
                    }

                    await Task.Delay(4000);
                }
            }


            return alivePlayers[0];
        }




        private async Task UpdatePlayersStatAsync(IGuildUser winner, HashSet<ulong> playersId)
        {
            var playersStat = await _db.RussianRouletteStats
                      .AsTracking()
                      .Where(stat => playersId.Contains(stat.UserId) && stat.GuildId == Context.Guild.Id)
                      .ToListAsync();

            var newPlayersId = playersId
                .Except(playersStat.Select(s => s.UserId))
                .ToList();

            if (newPlayersId.Count > 0)
            {
                var newPlayersStats = newPlayersId.Select(id => new RussianRouletteStats
                {
                    UserId = id,
                    GuildId = Context.Guild.Id
                })
                .ToList();

                await _db.RussianRouletteStats.AddRangeAsync(newPlayersStats);

                playersStat.AddRange(newPlayersStats);
            }

            foreach (var stat in playersStat)
                stat.GamesCount++;

            var winnerStat = playersStat.First(u => u.UserId == winner.Id);

            winnerStat.WinsCount++;


            await _db.SaveChangesAsync();
        }


        private async Task<RussianRoulleteData> CreateRussianRoulleteDataAsync()
        {
            var rrSettings = await GetSettingsAsync<RussianRouletteSettings>(_db, Context.Guild.Id, false);

            var rrdata = new RussianRoulleteData(new Emoji(rrSettings.UnicodeSmileKilled), new Emoji(rrSettings.UnicodeSmileSurvived))
            {
                CustomEmoteKilled = rrSettings.CustomSmileKilled is not null ? Emote.Parse(rrSettings.CustomSmileKilled) : null,
                CustomEmoteSurvived = rrSettings.CustomSmileSurvived is not null ? Emote.Parse(rrSettings.CustomSmileSurvived) : null,
            };

            return rrdata;
        }


        private class RussianRoulleteData
        {
            public Emoji UnicodeEmojiKilled { get; }
            public Emoji UnicodeEmojiSurvived { get; }

            public Emote? CustomEmoteKilled { get; init; }
            public Emote? CustomEmoteSurvived { get; init; }


            public RussianRoulleteData(Emoji unicodeEmojiKilled, Emoji unicodeEmojiSurvived)
            {
                UnicodeEmojiKilled = unicodeEmojiKilled;
                UnicodeEmojiSurvived = unicodeEmojiSurvived;
            }
        }
    }
}
