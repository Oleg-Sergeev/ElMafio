using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Data.Models;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Modules.Extensions;

namespace Modules.Games
{
    [Group("рулетка")]
    [Alias("р")]
    public class RussianRoulleteModule : GameModule
    {
        public RussianRoulleteModule(Random random, BotContext db) : base(random, db)
        {
        }


        protected override GameModuleData CreateGameData(IGuildUser creator)
            => new("Русская рулетка", 2, creator);


        public override async Task ResetStatAsync(IGuildUser guildUser)
            => await ResetStatAsync<RussianRouletteStats>(guildUser);



        public override async Task StartAsync()
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

            await Task.Delay(3000);


            var winner = await PlayAsync();


            if (GameData.IsPlaying && winner is not null)
            {
                await Task.Delay(2000);


                await ReplyAsync($"И абсолютным чемпионом русской рулетки становится {winner.Mention}! Поздравляем!");


                var delay = Task.Delay(1000);

                await AddNewUsersAsync(playersId);

                await delay;

                var updateStatsTask = UpdatePlayersStatAsync(winner, playersId);


                await ReplyAsync("Игра завершена!");


                await updateStatsTask;
            }


            DeleteGameData();
        }


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


        private async Task<IGuildUser?> PlayAsync()
        {
            await ReplyAsync("Крутим барабан...");


            var delay = Task.Delay(3000);

            GameData!.Players.Shuffle();

            await delay;


            var alivePlayers = new List<IGuildUser>(GameData.Players);

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

                        //if (Emote.TryParse("<:chel:795021357536378880>", out var emote)) 
                        //    await msg.AddReactionAsync(emote);

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

                        //if (Emote.TryParse("<:Obama:791309609817866262>", out var emote)) 
                        //    await msg.AddReactionAsync(emote);

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
    }
}
