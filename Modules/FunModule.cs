using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Modules.Extensions;

namespace Modules
{
    [Name("Фан")]
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;


        public FunModule(Random random)
        {
            _random = random;
        }


        [Command("шанс")]
        public async Task CalculateChanceAsync([Remainder] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await ReplyAsync("Пожалуйста, введите текст.");

                return;
            }

            int num = _random.Next(101);

            await ReplyAsync($"Шанс того, что {text} - {num}%");
        }


        [Command("эхо")]
        public async Task Echo([Remainder] string message)
        {
            await Context.Message.DeleteAsync();

            await ReplyAsync(message);
        }


        [Command("данет")]
        public async Task SayYesOrNo([Remainder] string _)
        {
            bool answer = _random.Next(2) > 0;

            if (answer) await ReplyAsync("Да");
            else await ReplyAsync("Нет");
        }


        [Command("кто")]
        public async Task SayWhoIs([Remainder] string message)
        {
            int num = _random.Next(0, Context.Guild.Users.Count);

            var user = Context.Guild.Users.ToList()[num];
            var nickname = user.Nickname ?? user.Username;

            await ReplyAsync($"{nickname} {message}");
        }

        [Command("Голосование")]
        [Alias("голос", "гс")]
        private async Task MakeVotingAsync([Remainder] string text)
        {
            var content = text.Split("|");

            var votingText = content[0];

            var points = new List<string>();
            var emotes = new List<IEmote>();

            if (content.Length == 1 || string.IsNullOrWhiteSpace(content[1]))
            {
                emotes.Add(new Emoji("✅"));
                emotes.Add(new Emoji("❌"));
            }
            else
            {
                content[1] = content[1].Trim();

                points.AddRange(content[1].Split());

                votingText += "\n";
                for (int i = 0; i < Math.Min(points.Count, 10); i++)
                {
                    votingText += $"{i + 1} - {points[i]}\n";

                    emotes.Add(new Emoji((i + 1).ConvertToSmile()));
                }
            }

            var response = await ReplyAsync(votingText);

            await response.AddReactionsAsync(emotes.ToArray());
        }

        [Command("Смайлы")]
        [Alias("буквы")]
        private async void TransferToLetterSmiles([Remainder] string text)
        {
            var letters = "";

            foreach (var letter in text[..Math.Min(25, text.Length)])
            {
                if (letter.TryConvertToSmile(out var smile)) letters += smile;
                else letters += ' ';

                letters += '\u202F';
            }

            if (!string.IsNullOrWhiteSpace(letters)) await ReplyAsync(letters);
        }

    }
}
