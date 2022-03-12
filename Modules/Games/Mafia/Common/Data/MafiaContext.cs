using System;
using System.Threading.Tasks;
using Core.Common.Data;
using Core.Extensions;
using Discord;
using Fergun.Interactive;
using Infrastructure.Data.Entities.Games.Settings.Mafia;
using Services;

namespace Modules.Games.Mafia.Common.Data;

public class MafiaContext
{
    public MafiaGuildData GuildData { get; }

    public MafiaRolesData RolesData { get; }

    public MafiaData MafiaData { get; }

    public MafiaSettings Settings { get; }

    public MafiaSettingsTemplate SettingsTemplate { get; }

    public DbSocketCommandContext CommandContext { get; }

    public InteractiveService Interactive { get; }


    public int VoteTime { get; }


    public MafiaContext(MafiaGuildData guildData, MafiaData mafiaData, MafiaSettings settings,
        DbSocketCommandContext commandContext, InteractiveService interactive)
    {
        if (settings.CurrentTemplate is null)
            throw new InvalidOperationException($"Mafia settings template cannot be null when creating a {nameof(MafiaContext)}. Parameter: {nameof(settings.CurrentTemplate)}");

        RolesData = new();

        Settings = settings;
        SettingsTemplate = settings.CurrentTemplate;
        VoteTime = settings.CurrentTemplate.GameSubSettings.VoteTime;

        GuildData = guildData;
        MafiaData = mafiaData;
        CommandContext = commandContext;
        Interactive = interactive;
    }


    public async Task ChangeMurdersPermsAsync(OverwritePermissions textPerms, OverwritePermissions? voicePerms)
    {
        foreach (var murder in RolesData.Murders.Values)
        {
            if (!murder.IsAlive)
                continue;

            var player = murder.Player;

            var currentTextPerms = GuildData.MurderTextChannel.GetPermissionOverwrite(player);

            if (!currentTextPerms.AreSame(textPerms))
                await GuildData.MurderTextChannel.AddPermissionOverwriteAsync(player, textPerms);


            if (GuildData.MurderVoiceChannel is null || voicePerms is not OverwritePermissions perms)
                continue;


            var currentVoicePerms = GuildData.MurderVoiceChannel.GetPermissionOverwrite(player);

            if (currentVoicePerms.AreSame(voicePerms))
                continue;


            await GuildData.MurderVoiceChannel.AddPermissionOverwriteAsync(player, perms);

            if (player.VoiceChannel != null && perms.ViewChannel == PermValue.Deny)
                await player.ModifyAsync(props => props.Channel = null);
        }
    }


    public async Task ChangeCitizenPermsAsync(OverwritePermissions textPerms, OverwritePermissions? voicePerms)
    {
        var currentTextPerms = GuildData.GeneralTextChannel.GetPermissionOverwrite(GuildData.MafiaRole);

        if (!currentTextPerms.AreSame(textPerms))
            await GuildData.GeneralTextChannel.AddPermissionOverwriteAsync(GuildData.MafiaRole, textPerms);


        if (GuildData.GeneralVoiceChannel is null || voicePerms is not OverwritePermissions perms)
            return;

        var currentVoicePerms = GuildData.GeneralVoiceChannel.GetPermissionOverwrite(GuildData.MafiaRole);

        if (currentVoicePerms.AreSame(voicePerms))
            return;

        await GuildData.GeneralVoiceChannel.AddPermissionOverwriteAsync(GuildData.MafiaRole, perms);

        if (perms.ViewChannel == PermValue.Deny)
            foreach (var role in RolesData.AliveRoles.Values)
            {
                var player = role.Player;

                if (player.VoiceChannel != null)
                    await player.ModifyAsync(props => props.Channel = null);
            }
    }

}
