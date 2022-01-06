using Core.Extensions;
using Discord;
using Microsoft.Extensions.Configuration;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common;

public static class MafiaHelper
{
    private const string RolesSection = "Games:Mafia:Roles";

    public static readonly OverwritePermissions DenyView = new(viewChannel: PermValue.Deny);

    public static OverwritePermissions GetAllowSpeak(IChannel channel)
        => OverwritePermissions.DenyAll(channel).Modify(
                viewChannel: PermValue.Allow,
                connect: PermValue.Allow,
                useVoiceActivation: PermValue.Allow,
                speak: PermValue.Allow
                );


    public static OverwritePermissions GetAllowWrite(IChannel channel)
        => OverwritePermissions.DenyAll(channel).Modify(
       viewChannel: PermValue.Allow,
       readMessageHistory: PermValue.Allow,
       sendMessages: PermValue.Allow);

    public static OverwritePermissions GetDenyWrite(IChannel channel)
        => OverwritePermissions.DenyAll(channel).Modify(
        viewChannel: PermValue.Allow,
        readMessageHistory: PermValue.Allow);


    public static Embed GetEmbed(GameRole role, IConfiguration config)
    {
        var type = role.GetType();

        var pair = config.GetEmbedFieldInfo($"{RolesSection}:{type.Name}");

        var embedBuilder = new EmbedBuilder()
            .WithTitle($"Ваша роль - {role.Name}")
            .WithDescription(pair?.Item2 ?? "*Описание отсутствует*");
            //.WithImageUrl($"url/{type}.png");

        return role switch
        {
            Doctor => embedBuilder.WithColor(Color.DarkBlue).Build(),
            Sheriff => embedBuilder.WithColor(Color.Blue).Build(),
            Innocent => embedBuilder.WithColor(Color.Green).Build(),
            Don => embedBuilder.WithColor(Color.DarkRed).Build(),
            Murder => embedBuilder.WithColor(Color.Red).Build(),
            Maniac => embedBuilder.WithColor(Color.LighterGrey).Build(),
            Hooker => embedBuilder.WithColor(Color.Purple).Build(),
            _ => embedBuilder.Build()
        };
    }
}
