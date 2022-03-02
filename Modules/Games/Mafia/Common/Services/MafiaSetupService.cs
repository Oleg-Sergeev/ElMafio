using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Core.Common;
using Core.Exceptions;
using Core.Extensions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.GameRoles.Data;

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

        var token = context.MafiaData.TokenSource.Token;

        var guildData = context.GuildData;
        var commandContext = context.CommandContext;

        var denyView = MafiaHelper.DenyView;
        var denyWrite = MafiaHelper.DenyWrite;

        var bot = commandContext.Guild.GetUser(903248920099057744);

        foreach (var channel in commandContext.Guild.Channels)
        {
            var hasPerm = bot.GetPermissions(channel).ViewChannel && channel.GetPermissionOverwrite(bot)?.ManageRoles != PermValue.Deny;

            if (!hasPerm)
                continue;

            if (channel.Id == guildData.GeneralTextChannel.Id)
            {
                var mafiaPerms = channel.GetPermissionOverwrite(guildData.MafiaRole);

                if (!mafiaPerms?.AreSame(denyWrite) ?? true)
                    tasks.Add(AddPermsAsync(denyWrite));

                continue;
            }

            var perms = channel.GetPermissionOverwrite(guildData.MafiaRole);

            if (perms?.ViewChannel == PermValue.Deny)
                continue;

            tasks.Add(AddPermsAsync(denyView));



            async Task AddPermsAsync(OverwritePermissions perms)
            {
                try
                {
                    await channel.AddPermissionOverwriteAsync(guildData.MafiaRole, perms, new() { CancelToken = token });
                }
                catch (HttpException e)
                {
                    await HandleHttpExceptionAsync($"Не удалось добавить переопределение для канала `{channel.Name}`", e, context);
                }
            }
        }


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
                var allowWrite = MafiaHelper.AllowWrite;

                var specPerms = guildData.SpectatorTextChannel.GetPermissionOverwrite(guildData.SpectatorRole);
                if (!Equals(specPerms, allowWrite))
                    await guildData.SpectatorTextChannel.AddPermissionOverwriteAsync(guildData.SpectatorRole, allowWrite);
            }

            if (guildData.SpectatorVoiceChannel is not null)
            {
                var allowSpeak = MafiaHelper.AllowSpeak;

                var specPerms = guildData.SpectatorVoiceChannel.GetPermissionOverwrite(guildData.SpectatorRole);

                if (!Equals(specPerms, allowSpeak))
                    await guildData.SpectatorVoiceChannel.AddPermissionOverwriteAsync(guildData.SpectatorRole, allowSpeak);
            }
        }


        if (!guildData.GeneralTextChannel.GetPermissionOverwrite(commandContext.Guild.EveryoneRole)?.AreSame(denyView) ?? true)
            await guildData.GeneralTextChannel.AddPermissionOverwriteAsync(commandContext.Guild.EveryoneRole, denyView);

        if (!guildData.MurderTextChannel.GetPermissionOverwrite(commandContext.Guild.EveryoneRole)?.AreSame(denyView) ?? true)
            await guildData.MurderTextChannel.AddPermissionOverwriteAsync(commandContext.Guild.EveryoneRole, denyView);


        Task.WaitAll(tasks.ToArray(), token);
    }

    public async Task SetupUsersAsync(MafiaContext context)
    {
        var tasks = new List<Task>();

        var token = context.MafiaData.TokenSource.Token;

        foreach (var player in context.MafiaData.Players)
            tasks.Add(Task.Run(() => HandlePlayerAsync(player), token));

        await Task.WhenAll(tasks);

        async Task HandlePlayerAsync(IGuildUser player)
        {
            var serverSettings = context.SettingsTemplate.ServerSubSettings;

            var guildData = context.GuildData;


            await guildData.MurderTextChannel.RemovePermissionOverwriteAsync(player, new() { CancelToken = token });

            guildData.PlayerRoleIds.Add(player.Id, new List<ulong>());


            var guildPlayer = (SocketGuildUser)player;

            if (serverSettings.RenameUsers && guildPlayer.Nickname is null && guildPlayer.Id != context.CommandContext.Guild.OwnerId)
            {
                try
                {
                    await guildPlayer.ModifyAsync(props => props.Nickname = $"_{guildPlayer.Username}_", new() { CancelToken = token });

                    guildData.OverwrittenNicknames.Add(guildPlayer.Id);
                }
                catch (HttpException e)
                {
                    await HandleHttpExceptionAsync($"Не удалось сменить ник пользователю {guildPlayer.GetFullMention()}", e, context);
                }
            }


            if (serverSettings.RemoveRolesFromUsers)
            {
                var playerRoles = guildPlayer.Roles
                    .Where(role => !role.IsEveryone && role.Id != guildData.MafiaRole.Id && role.Id != (guildData.SpectatorRole?.Id ?? 0));

                foreach (var role in playerRoles)
                {
                    try
                    {
                        await guildPlayer.RemoveRoleAsync(role, new() { CancelToken = token });

                        guildData.PlayerRoleIds[player.Id].Add(role.Id);
                    }
                    catch (HttpException e)
                    {
                        await HandleHttpExceptionAsync($"Не удалось убрать роль {role.Mention} с пользователя {guildPlayer.GetFullMention()}", e, context);
                    }
                }
            }

            await player.AddRoleAsync(guildData.MafiaRole, new() { CancelToken = token });
        }
    }

    public void SetupRoles(MafiaContext context)
    {
        int offset = 0;

        var template = context.SettingsTemplate;
        var mafiaData = context.MafiaData;
        var rolesData = context.RolesData;


        var rolesInfo = template.RolesExtraInfoSubSettings;
        var gameSettings = template.GameSubSettings;

        var isCustomGame = gameSettings.IsCustomGame;
        var roleAmount = template.RoleAmountSubSettings;


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
                throw new WrongPlayersCountException("Mismatch in the number of roles and players", mafiaData.Players.Count, rolesCount);


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
            var doctor = new Doctor(mafiaData.Players[offset], _gameRoleData, rolesInfo.DoctorSelfHealsCount);

            rolesData.AddSingleRole(doctor);
        }

        for (int i = 0; i < sheriffsCount; i++, offset++)
        {
            var sheriff = new Sheriff(mafiaData.Players[offset], _gameRoleData, rolesInfo.SheriffShotsCount, rolesData.Murders.Values);


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

        var token = context.MafiaData.TokenSource.Token;

        foreach (var role in context.RolesData.AllRoles.Values)
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await role.Player.SendMessageAsync(embed: MafiaHelper.GetEmbed(role, _config), options: new() { CancelToken = token });
                }
                catch (HttpException e)
                {
                    await HandleHttpExceptionAsync($"Не удалось отправить сообщение пользователю {role.Player.GetFullMention()}", e, context);
                }
            }, token));


        await Task.WhenAll(tasks);
    }

    public async Task SendWelcomeMessageAsync(MafiaContext context)
    {
        var tasks = new List<Task>();

        var token = context.MafiaData.TokenSource.Token;

        foreach (var role in context.RolesData.AllRoles.Values)
            tasks.Add(Task.Run(async () =>
            {
                var welcomeMsg = "**Добро пожаловать в Мафию!**\nСкоро я вышлю вам вашу роль и вы начнете играть";

                try
                {
                    await role.Player.SendMessageAsync(embed: EmbedHelper.CreateEmbed(welcomeMsg, "Мафия"), options: new() { CancelToken = token });
                }
                catch (HttpException e)
                {
                    await HandleHttpExceptionAsync($"Не удалось отправить сообщение пользователю {role.Player.GetFullMention()}", e, context);
                }
            }, token));


        await Task.WhenAll(tasks);
    }


    private static async Task HandleHttpExceptionAsync(string message, HttpException e, MafiaContext context)
    {
        if (context.SettingsTemplate.ServerSubSettings.ReplyMessagesOnSetupError)
            await context.CommandContext.Channel.SendEmbedAsync(message, EmbedStyle.Error);

        if (context.SettingsTemplate.ServerSubSettings.AbortGameWhenError)
            throw new GameSetupAbortedException(message, e);
    }
}
