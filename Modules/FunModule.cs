using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Modules.Extensions;

namespace Modules;

[Name("Фан")]
public class FunModule : GuildModuleBase
{
    private readonly Random _random;


    public FunModule(Random random)
    {
        _random = random;
    }


    [Command("Шанс")]
    public async Task CalculateChanceAsync([Remainder] string text)
    {
        int num = _random.Next(101);

        await ReplyAsync($"Шанс того, что {text} - **{num}%**", messageReference: new(Context.Message.Id));
    }


    [Command("Эхо")]
    public async Task Echo([Remainder] string message)
    {
        await Context.Message.DeleteAsync();

        await ReplyAsync(message, messageReference: Context.Message.Reference);
    }


    [Command("Данет")]
    public async Task SayYesOrNo()
    {
        bool answer = _random.Next(2) > 0;

        if (answer)
            await ReplyAsync("**Да**");
        else
            await ReplyAsync("**Нет**");
    }


    [Command("Кто")]
    public async Task SayWhoIs([Remainder] string? message = null)
    {
        int num = _random.Next(0, Context.Guild.Users.Count);

        var user = Context.Guild.Users.ToList()[num];
        var nickname = user.Nickname ?? user.Username;

        await ReplyAsync($"**{nickname}** {message}", messageReference: new(Context.Message.Id));
    }

    [Command("Голосование")]
    [Alias("голос", "гс")]
    public async Task MakeVotingAsync([Remainder] string text)
    {
        var content = text.Split("|");

        string? title = null;

        if (!string.IsNullOrEmpty(content[0]))
            title = content[0];

        var options = "";

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

            for (int i = 0; i < Math.Min(points.Count, 20); i++)
            {
                options += $"{(char)(i + 65)} - {points[i]}\n";

                emotes.Add(new Emoji(((char)(i + 65)).ConvertToSmile()));
            }
        }

        var response = await ReplyEmbedAsync(EmbedType.Information, options, false, title, withDefaultAuthor: true);

        await response.AddReactionsAsync(emotes.ToArray());
    }

    [Command("Буквы")]
    public async Task TransferToLetterSmilesAsync([Remainder] string text)
    {
        var letters = "";

        foreach (var letter in text[..Math.Min(25, text.Length)])
        {
            if (letter.TryConvertToSmile(out var smile))
                letters += smile;
            else
                letters += ' ';

            letters += '\u202F';
        }

        if (!string.IsNullOrWhiteSpace(letters))
            await ReplyAsync(letters, messageReference: Context.Message.Reference);
    }

    [Command("Эхобуквы")]
    public async Task TransferToLetterSmilesAsyncAndEcho([Remainder] string text)
    {
        await TransferToLetterSmilesAsync(text);

        await Context.Message.DeleteAsync();
    }


    [Command("Аватар")]
    [Priority(0)]
    public async Task GetAvatarAsync()
        => await GetAvatarAsync(Context.User);

    [Command("Аватар")]
    [Priority(1)]
    public async Task GetAvatarAsync(IUser user)
    {
        var request = user.GetAvatarUrl(size: 2048);

        var resp = await new HttpClient().GetAsync(request);
        var stream = await resp.Content.ReadAsStreamAsync();

        if (resp is null)
        {
            await ReplyEmbedAsync(EmbedType.Information, "Не удалось загрузить аватар");

            return;
        }

        var avatarExtension = resp.Content.Headers.ContentType!.MediaType!.Split('/')[1];

        await Context.Channel.SendFileAsync(stream, $"avatar.{avatarExtension}");
    }


    [Command("Смайл")]
    public async Task GetSmileAsync(string emoteId)
    {
        if (Emote.TryParse(emoteId, out var emote))
            await GetSmileByIdAsync(emote.Id);
        else
            await ReplyEmbedAsync(EmbedType.Information, "Не удалось распознать пользовательский смайлик");
    }


    [Command("Смайлайди")]
    public async Task GetSmileByIdAsync(ulong smileId)
    {
        var resp = await new HttpClient().GetAsync($"https://cdn.discordapp.com/emojis/{smileId}.png")
            ?? await new HttpClient().GetAsync($"https://cdn.discordapp.com/emojis/{smileId}.gif");

        if (resp.IsSuccessStatusCode)
        {
            var stream = await resp.Content.ReadAsStreamAsync();

            var smileExtension = resp.Content.Headers.ContentType!.MediaType!.Split('/')[1];

            await Context.Channel.SendFileAsync(stream, $"smile.{smileExtension}");

            return;
        }

        await ReplyEmbedAsync(EmbedType.Information, "Не удалось найти пользовательский смайлик");
    }


    [Command("Смайлдобавить")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task AddEmoteAsync()
        => await AddEmoteAsync($"emoji_{Context.Guild.Emotes.Count + 1}");

    [Command("Смайлдобавить")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task AddEmoteAsync(string name)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault() ?? Context.Message.ReferencedMessage?.Attachments.FirstOrDefault();

        if (attachment is null)
        {
            await ReplyEmbedAsync(EmbedType.Error, "Прикрепите к своему сообщению картинку, или ответьте на сообщение, содержащее картинку");

            return;
        }

        var stream = await new HttpClient().GetStreamAsync(attachment.Url);

        if (stream is null)
        {
            await ReplyEmbedAsync(EmbedType.Error, "Не удалось загрузить картинку");

            return;
        }

        var emote = await Context.Guild.CreateEmoteAsync(name, new Image(stream));


        var msg = await ReplyEmbedAsync(EmbedType.Successfull, "Смайл успешно добавлен");

        await msg.AddReactionAsync(emote);
    }
}