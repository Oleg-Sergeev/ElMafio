using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common.Services;

public class MafiaSetupService : IMafiaSetupService
{
    private readonly IConfiguration _config;


    public MafiaSetupService(IConfiguration config)
    {
        _config = config;
    }


    public async Task SetupGuildAsync(MafiaContext context)
    {
        var tasks = new List<Task>();

        var guildData = context.GuildData;
        var commandContext = context.CommandContext;

        var denyView = MafiaHelper.DenyView;
        var denyWrite = MafiaHelper.GetDenyWrite(guildData.GeneralTextChannel);

        foreach (var channel in commandContext.Guild.Channels)
        {
            if (channel.Id == guildData.GeneralTextChannel.Id)
                continue;

            var perms = channel.GetPermissionOverwrite(guildData.MafiaRole);

            if (perms?.ViewChannel == PermValue.Deny)
                continue;

            tasks.Add(channel.AddPermissionOverwriteAsync(guildData.MafiaRole, denyView));
        }

        await Task.WhenAll(tasks);


        if (guildData.SpectatorRole is not null)
        {
            var generalTextPerms = guildData.GeneralTextChannel.GetPermissionOverwrite(guildData.SpectatorRole);
            var murderTextPerms = guildData.MurderTextChannel.GetPermissionOverwrite(guildData.SpectatorRole);

            if (!Equals(generalTextPerms, denyWrite))
                await guildData.GeneralTextChannel.AddPermissionOverwriteAsync(guildData.SpectatorRole, denyWrite);

            if (!Equals(murderTextPerms, denyWrite))
                await guildData.MurderTextChannel.AddPermissionOverwriteAsync(guildData.SpectatorRole, denyWrite);

            if (guildData.SpectatorTextChannel is not null)
            {
                var allowWrite = MafiaHelper.GetAllowWrite(guildData.SpectatorTextChannel);

                var specPerms = guildData.SpectatorTextChannel.GetPermissionOverwrite(guildData.SpectatorRole);
                if (!Equals(specPerms, allowWrite))
                    await guildData.SpectatorTextChannel.AddPermissionOverwriteAsync(guildData.SpectatorRole, allowWrite);
            }

            if (guildData.SpectatorVoiceChannel is not null)
            {
                var allowSpeak = MafiaHelper.GetAllowSpeak(guildData.SpectatorVoiceChannel);

                var specPerms = guildData.SpectatorVoiceChannel.GetPermissionOverwrite(guildData.SpectatorRole);

                if (!Equals(specPerms, allowSpeak))
                    await guildData.SpectatorVoiceChannel.AddPermissionOverwriteAsync(guildData.SpectatorRole, allowSpeak);
            }
        }


        if (!Equals(guildData.GeneralTextChannel.GetPermissionOverwrite(commandContext.Guild.EveryoneRole), denyView))
            await guildData.GeneralTextChannel.AddPermissionOverwriteAsync(commandContext.Guild.EveryoneRole, denyView);

        if (!Equals(guildData.MurderTextChannel.GetPermissionOverwrite(commandContext.Guild.EveryoneRole), denyView))
            await guildData.MurderTextChannel.AddPermissionOverwriteAsync(commandContext.Guild.EveryoneRole, denyView);



        static bool Equals(OverwritePermissions? o1, OverwritePermissions? o2)
            => o1?.AllowValue == o2?.AllowValue && o1?.DenyValue == o2?.DenyValue;
    }

    public async Task SetupUsersAsync(MafiaContext context)
    {
        var tasks = new List<Task>();

        foreach (var player in context.MafiaData.Players)
            tasks.Add(Task.Run(() => HandlePlayerAsync(player)));

        await Task.WhenAll(tasks);



        async Task HandlePlayerAsync(IGuildUser player)
        {
            var serverSettings = context.Settings.Current.ServerSubSettings;

            var guildData = context.GuildData;


            await guildData.MurderTextChannel.RemovePermissionOverwriteAsync(player);

            guildData.PlayerRoleIds.Add(player.Id, new List<ulong>());


            var guildPlayer = (SocketGuildUser)player;

            if (false && serverSettings.RenameUsers && guildPlayer.Nickname is null && guildPlayer.Id != context.CommandContext.Guild.OwnerId)
            {
                await guildPlayer.ModifyAsync(props => props.Nickname = $"_{guildPlayer.Username}_");

                guildData.OverwrittenNicknames.Add(guildPlayer.Id);
            }


            if (false && serverSettings.RemoveRolesFromUsers)
            {
                var playerRoles = guildPlayer.Roles
                    .Where(role => !role.IsEveryone && role.Id != guildData.MafiaRole.Id && role.Id != (guildData.SpectatorRole?.Id ?? 0));

                foreach (var role in playerRoles)
                {
                    await guildPlayer.RemoveRoleAsync(role);

                    guildData.PlayerRoleIds[player.Id].Add(role.Id);
                }
            }

            await player.AddRoleAsync(guildData.MafiaRole);
        }
    }

    public void SetupRoles(MafiaContext context)
    {
        int offset = 0;

        var settings = context.Settings;
        var mafiaData = context.MafiaData;
        var roleData = context.RolesData;


        var rolesInfo = settings.Current.RolesInfoSubSettings;
        var isCustomGame = settings.Current.GameSubSettings.IsCustomGame;
        var mafiaCoefficient = settings.Current.GameSubSettings.MafiaCoefficient;
        var roleAmount = settings.Current.RoleAmountSubSettings;

        var blackRolesCount = Math.Max(mafiaData.Players.Count / mafiaCoefficient, 1);
        var redRolesCount = Math.Max((int)(blackRolesCount / 2.5f), 1);


        int donsCount;
        int doctorsCount;
        int murdersCount;
        int sheriffsCount;

        if (isCustomGame)
        {
            var neutralsCount = roleAmount.NeutralRolesCount ?? 0;

            var exceptInnocentsCount = mafiaData.Players.Count - roleAmount.InnocentCount;

            var redRolesRemainsCount = exceptInnocentsCount - (roleAmount.BlackRolesCount ?? blackRolesCount) - neutralsCount;


            doctorsCount = roleAmount.DoctorsCount ?? Math.Min(redRolesRemainsCount ?? redRolesCount, redRolesCount);

            sheriffsCount = roleAmount.SheriffsCount ?? Math.Min(redRolesRemainsCount - doctorsCount ?? redRolesCount, redRolesCount);


            murdersCount = roleAmount.MurdersCount ?? exceptInnocentsCount - doctorsCount - sheriffsCount - neutralsCount ?? blackRolesCount;

            donsCount = roleAmount.DonsCount ?? (murdersCount > 2 ? 1 : 0);

            if (roleAmount.DonsCount is null)
                murdersCount -= donsCount;

        }
        else
        {
            doctorsCount = redRolesCount;

            sheriffsCount = redRolesCount;

            murdersCount = blackRolesCount;

            donsCount = murdersCount > 2 ? 1 : 0;
        }


        for (int i = 0; i < murdersCount; i++, offset++)
        {
            var murder = new Murder(mafiaData.Players[offset], context.GameRoleOptions);

            roleData.AllRoles.Add(murder.Player, murder);
        }

        for (int i = 0; i < donsCount; i++, offset++)
        {
            var don = new Don(mafiaData.Players[offset], context.GameRoleOptions, roleData.Sheriffs.Values);

            roleData.AllRoles.Add(don.Player, don);
        }


        if (isCustomGame)
        {
            for (int i = 0; i < roleAmount.ManiacsCount; i++, offset++)
            {
                var maniac = new Maniac(mafiaData.Players[offset], context.GameRoleOptions);

                roleData.AllRoles.Add(maniac.Player, maniac);
            }


            for (int i = 0; i < roleAmount.HookersCount; i++, offset++)
            {
                var hooker = new Hooker(mafiaData.Players[offset], context.GameRoleOptions);

                roleData.AllRoles.Add(hooker.Player, hooker);
            }
        }



        for (int i = 0; i < doctorsCount; i++, offset++)
        {
            var doctor = new Doctor(mafiaData.Players[offset], context.GameRoleOptions, rolesInfo.DoctorSelfHealsCount ?? 1);

            roleData.AllRoles.Add(doctor.Player, doctor);
        }


        for (int i = 0; i < sheriffsCount; i++, offset++)
        {
            var sheriff = new Sheriff(mafiaData.Players[offset], context.GameRoleOptions, rolesInfo.SheriffShotsCount ?? 0, roleData.Murders.Values);


            roleData.AllRoles.Add(sheriff.Player, sheriff);
        }



        for (int i = offset; i < mafiaData.Players.Count; i++)
        {
            var innocent = new Innocent(mafiaData.Players[i], context.GameRoleOptions);

            roleData.AllRoles.Add(innocent.Player, innocent);
        }
    }

    public async Task SendRolesInfoAsync(MafiaContext context)
    {
        var tasks = new List<Task>();

        foreach (var role in context.RolesData.AllRoles.Values)
            tasks.Add(role.Player.SendMessageAsync(embed: MafiaHelper.GetEmbed(role, _config)));


        await Task.WhenAll(tasks);
    }
}
