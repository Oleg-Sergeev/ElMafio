using System.Globalization;
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

        var roleInfo = config.GetSectionFields($"{RolesSection}:{type.Name}");

        var description = "*Описание отсутствует*";
        var color = Color.LightGrey;

        if (roleInfo is not null)
        {
            if (roleInfo.ContainsKey("Description"))
                description = roleInfo["Description"];

            if (roleInfo.TryGetValue("Color", out var colorStr) && uint.TryParse(colorStr, NumberStyles.HexNumber, null, out var rawColor))
                color = new Color(rawColor);
        }

        var embedBuilder = new EmbedBuilder()
            .WithTitle($"Ваша роль - {role.Name}")
            .WithDescription(description)
            .WithColor(color);


        var imageUrl = role switch
        {
            Doctor => "https://i.ibb.co/q06pwRw/Doctor.png",
            Sheriff => "https://i.ibb.co/6nW0Tg9/Sheriff.png",
            Innocent => "https://i.ibb.co/VBrcYJ4/Citizen.png",
            Don => "https://i.ibb.co/YXz3ScH/Don.png",
            Murder => "https://i.ibb.co/Xbdpxr0/Murder.png",
            Hooker => "https://i.ibb.co/fNvvFwc/Hooker.png",
            Maniac => "https://i.ibb.co/k9PsZMJ/Maniac.png",
            _ => null
        };

        if (imageUrl is not null)
            embedBuilder.WithImageUrl(imageUrl);

        return embedBuilder.Build();
    }
}
