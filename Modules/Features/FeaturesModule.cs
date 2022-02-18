using System;
using System.Globalization;
using System.Threading.Tasks;
using Core.Common;
using Core.Resources;
using Discord;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Features;

[Group("ф")]
[RequireOwner]
public class FeaturesModule : GuildModuleBase
{
    public FeaturesModule(InteractiveService interactiveService) : base(interactiveService)
    {
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
}