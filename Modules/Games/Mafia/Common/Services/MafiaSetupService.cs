using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.GameRoles.RolesGroups;

namespace Modules.Games.Mafia.Common.Services;

public class MafiaSetupService : IMafiaSetupService
{
    private readonly IConfiguration _config;
    private readonly IOptionsSnapshot<GameRoleData> _gameRoleData;


    public MafiaSetupService(IConfiguration config, IOptionsSnapshot<GameRoleData> gameRoleData)
    {
        _config = config;
        _gameRoleData = gameRoleData;
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

            if (serverSettings.RenameUsers && guildPlayer.Nickname is null && guildPlayer.Id != context.CommandContext.Guild.OwnerId)
            {
                await guildPlayer.ModifyAsync(props => props.Nickname = $"_{guildPlayer.Username}_");

                guildData.OverwrittenNicknames.Add(guildPlayer.Id);
            }


            if (serverSettings.RemoveRolesFromUsers)
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
        var rolesData = context.RolesData;


        var rolesInfo = settings.Current.RolesExtraInfoSubSettings;
        var gameSettings = settings.Current.GameSubSettings;

        var isCustomGame = gameSettings.IsCustomGame;
        var roleAmount = settings.Current.RoleAmountSubSettings;


        int donsCount;
        int doctorsCount;
        int murdersCount;
        int sheriffsCount;
        int innocentsCount;

        if (isCustomGame)
        {
            donsCount = roleAmount.DonsCount;
            doctorsCount = roleAmount.DoctorsCount;
            sheriffsCount = roleAmount.SheriffsCount;
            murdersCount = roleAmount.MurdersCount;
            innocentsCount = roleAmount.InnocentsCount;

            var rolesCount = donsCount + doctorsCount + murdersCount + sheriffsCount + innocentsCount + (isCustomGame ? roleAmount.NeutralRolesCount : 0);
            if (rolesCount > mafiaData.Players.Count)
                throw new WrongPlayersCountException("Inconsistency in the number of roles and players", mafiaData.Players.Count, rolesCount);

            if (!gameSettings.IsFillWithMurders)
                innocentsCount = mafiaData.Players.Count - doctorsCount - sheriffsCount - roleAmount.BlackRolesCount - roleAmount.NeutralRolesCount;
            else
                murdersCount = mafiaData.Players.Count - roleAmount.RedRolesCount - donsCount - roleAmount.NeutralRolesCount;
        }
        else
        {
            var blackRolesCount = Math.Max(mafiaData.Players.Count / gameSettings.MafiaCoefficient, 1);
            var redRolesCount = Math.Max((int)(blackRolesCount / 2.5f), 1);

            doctorsCount = redRolesCount;

            sheriffsCount = redRolesCount;


            murdersCount = blackRolesCount;

            donsCount = murdersCount > 2 ? 1 : 0;
            murdersCount -= donsCount;

            innocentsCount = mafiaData.Players.Count - doctorsCount - sheriffsCount - murdersCount - donsCount;
        }


        for (int i = 0; i < murdersCount; i++, offset++)
        {
            var murder = new Murder(mafiaData.Players[offset], _gameRoleData);

            rolesData.AddSingleRole(murder);
        }

        for (int i = 0; i < donsCount; i++, offset++)
        {
            var don = new Don(mafiaData.Players[offset], _gameRoleData, rolesData.Sheriffs.Values);

            rolesData.AddSingleRole(don);
        }


        if (isCustomGame)
        {
            for (int i = 0; i < roleAmount.ManiacsCount; i++, offset++)
            {
                var maniac = new Maniac(mafiaData.Players[offset], _gameRoleData);

                rolesData.AddSingleRole(maniac);
            }


            for (int i = 0; i < roleAmount.HookersCount; i++, offset++)
            {
                var hooker = new Hooker(mafiaData.Players[offset], _gameRoleData);

                rolesData.AddSingleRole(hooker);
            }
        }



        for (int i = 0; i < doctorsCount; i++, offset++)
        {
            var doctor = new Doctor(mafiaData.Players[offset], _gameRoleData, rolesInfo.DoctorSelfHealsCount ?? 1);

            rolesData.AddSingleRole(doctor);
        }

        for (int i = 0; i < sheriffsCount; i++, offset++)
        {
            var sheriff = new Sheriff(mafiaData.Players[offset], _gameRoleData, rolesInfo.SheriffShotsCount ?? 0, rolesData.Murders.Values);


            rolesData.AddSingleRole(sheriff);
        }


        for (int i = 0; i < innocentsCount; i++, offset++)
        {
            var innocent = new Innocent(mafiaData.Players[offset], _gameRoleData);
            rolesData.AddSingleRole(innocent);
        }


        rolesData.AssignRoles();

        var citizen = new CitizenGroup(rolesData.AllRoles.Values.ToList(), _gameRoleData);
        var murdersGroup = new MurdersGroup(rolesData.Murders.Values.ToList(), _gameRoleData);

        rolesData.AddGroupRole(citizen);
        rolesData.AddGroupRole(murdersGroup);
    }

    public async Task SendRolesInfoAsync(MafiaContext context)
    {
        var tasks = new List<Task>();

        foreach (var role in context.RolesData.AllRoles.Values)
            tasks.Add(role.Player.SendMessageAsync(embed: MafiaHelper.GetEmbed(role, _config)));


        await Task.WhenAll(tasks);
    }
}
