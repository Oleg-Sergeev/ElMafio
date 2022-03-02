using System.Threading.Tasks;
using Core.Common;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Services;

namespace Modules;

public class InteractionGuildModuleBase : InteractionModuleBase<DbInteractionContext>
{
    public InteractiveService Interactive { get; }

    public InteractionGuildModuleBase(InteractiveService interactive)
    {
        Interactive = interactive;
    }



    public Task RespondEmbedAsync(string description, string title, EmbedBuilder? embedBuilder = null, bool ephemeral = true)
        => RespondEmbedAsync(description, EmbedStyle.Information, title, embedBuilder);

    public Task RespondEmbedAsync(string description, EmbedStyle embedStyle = EmbedStyle.Information, string? title = null, EmbedBuilder? embedBuilder = null, bool ephemeral = true)
        => Context.Interaction.RespondAsync(embed: EmbedHelper.CreateEmbed(description, embedStyle, title, embedBuilder), ephemeral: ephemeral);

}