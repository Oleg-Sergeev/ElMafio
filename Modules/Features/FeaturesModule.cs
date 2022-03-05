using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Resources;
using Discord;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Features;

[Group("Фичи")]
[Alias("Ф")]
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