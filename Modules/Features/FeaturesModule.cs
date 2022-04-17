using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Core.Common;
using Core.Resources;
using Discord;
using Discord.Audio.Streams;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Features;

[Group("Фичи")]
[Alias("Ф")]
[RequireOwner]
public class FeaturesModule : CommandGuildModuleBase
{
    private readonly CommandService _commandService;
    private readonly IServiceProvider _serviceProvider;

    public FeaturesModule(InteractiveService interactiveService, CommandService commandService, IServiceProvider serviceProvider) : base(interactiveService)
    {
        _commandService = commandService;
        _serviceProvider = serviceProvider;
    }

    [Command("сейв")]
    public async Task Sace()
    {
        var p = Context.Guild.GetUser(184316176007036928);

        await p.AddRoleAsync(946125842491273296);
    }

    [Command("тест")]
    public async Task EphemeralTestAsync()
    {
        var entryVoteComponent = new ComponentBuilder()
               .WithButton("Голосовать", "vote")
               .Build();

        var entryVoteMsg = await ReplyAsync(embed: EmbedHelper.CreateEmbed("Для участия в голосовании нажмите на кнопку ниже"),
            components: entryVoteComponent);

        var res = await Interactive.NextMessageComponentAsync(m => m.Message.Id == entryVoteMsg.Id);

        if (res.IsSuccess)
        {
            var interaction = res.Value;

            await interaction.DeferAsync(true);


            IUserMessage? msg = null;
            int n = 0;
            while (true)
            {
                var component = new ComponentBuilder()
                       .WithButton("Проголосовать", "vote")
                       .WithButton("Пропустить", "skip", ButtonStyle.Danger)
                       .Build();

                var embed = new EmbedBuilder()
                    .WithDescription($"Выберите игрока из списка {n}")
                    .WithColor(Color.Gold)
                    .Build();


                if (msg is null)
                    msg = await interaction.FollowupAsync(embed: embed, components: component, ephemeral: true);
                else
                {
                    try
                    {
                        await msg.ModifyAsync(x =>
                        {
                            x.Embed = embed;
                            x.Components = component;
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"************************\n{e.Message}{e.StackTrace}\n****************************");

                        return;
                    }
                }

                var result = await Interactive.NextMessageComponentAsync(
                    x => x.Message.Id == msg.Id && x.User.Id == Context.Guild.OwnerId);

                if (result.IsSuccess)
                {
                    var data = result.Value.Data;

                    await result.Value.DeferAsync(true);

                    n++;

                    if (data.Type == ComponentType.Button)
                    {
                        if (data.CustomId == "skip")
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    [LocalizedCommand("Hello")]
    public async Task LocaleTestAsync()
    {
        await ReplyAsync(Resource.Hello);
    }

    [Command("п")]
    public async Task ReloadModulesAsync(string lang = "ru")
    {
        var res = await _commandService.RemoveModuleAsync<FeaturesModule>();

        if (!res)
        {
            await ReplyAsync("Fail");

            return;
        }

        var t = Resource.Culture.Name;

        Resource.Culture = CultureInfo.GetCultureInfo(lang);

        await _commandService.AddModuleAsync<FeaturesModule>(_serviceProvider);

        Resource.Culture = CultureInfo.GetCultureInfo(t);

        await ReplyAsync("Success");
    }


    [Command("с")]
    public async Task CompressImageAsync(IUser? user = null)
    {
        user ??= Context.User;

        var request = user.GetAvatarUrl(size: 2048);

        var avatarResponse = await new HttpClient().GetAsync(request);

        if (avatarResponse is null)
        {
            await ReplyEmbedAsync("Не удалось загрузить аватар", EmbedStyle.Error);

            return;
        }

        var avatarExtension = avatarResponse.Content.Headers.ContentType?.MediaType?.Split('/')?[1];

        if (avatarExtension is null)
        {
            await ReplyEmbedAsync("Не удалось загрузить аватар: не найдено расширение картинки", EmbedStyle.Error);

            return;
        }


        using HttpClient client = new();

        var avatarStream = await avatarResponse.Content.ReadAsStreamAsync();

        MultipartFormDataContent form = new();
        form.Add(new StreamContent(avatarStream), "uploadfile", "avatar.png");
        form.Add(new StringContent("1"), "sizeperc");
        form.Add(new StringContent("256"), "kbmbsize");
        form.Add(new StringContent("1"), "kbmb");
        form.Add(new StringContent("1"), "mpxopt");
        form.Add(new StringContent("1"), "jpegtype");
        form.Add(new StringContent("1"), "jpegmeta");

        var result = await client.PostAsync("https://www.imgonline.com.ua/compress-image-size-result.php", form);

        var htmlStream= await result.Content.ReadAsStreamAsync();

        string content;

        using (StreamReader reader = new(htmlStream))
            content = reader.ReadToEnd();

        var href = content[content.IndexOf("https")..];

        var imgUrl = href[..(href.IndexOf(".jpg") + 4)];

        var compressedAvatar = await new HttpClient().GetAsync(imgUrl);

        var compressedAvatarStream = await compressedAvatar.Content.ReadAsStreamAsync();

        await Context.Channel.SendFileAsync(compressedAvatarStream, $"compressed_avatar.jpg");
    }
}


public class LocalizedCommandAttribute : CommandAttribute
{
    public LocalizedCommandAttribute(string key) : base(key)
    {
        var type = GetType().BaseType;

        if (type is null)
            return;

        var fieldText = type.GetField("<Text>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        if (fieldText is not null)
        {
            var localizedCommandName = Resource.ResourceManager.GetString(key, Resource.Culture);

            if (localizedCommandName is not null)
                fieldText.SetValue(this, localizedCommandName);
        }
    }
}