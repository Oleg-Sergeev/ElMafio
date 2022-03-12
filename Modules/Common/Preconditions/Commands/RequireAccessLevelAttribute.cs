using System;
using System.Threading.Tasks;
using Core.Extensions;
using Discord.Commands;
using Infrastructure.Data;
using Infrastructure.Data.Entities;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Common.Preconditions.Commands;


public class RequireStandartAccessLevelAttribute : PreconditionAttribute
{
    private readonly StandartAccessLevel _accessLevel;


    public RequireStandartAccessLevelAttribute(StandartAccessLevel accessLevel)
    {
        _accessLevel = accessLevel;
    }


    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var db = services.GetService<BotContext>();

        if (db is null)
            return PreconditionResult.FromSuccess();

        var cache = services.GetService<IMemoryCache>();


        if (cache is null || !cache.TryGetValue((context.Guild.Id, context.User.Id), out ServerUser? serverUser))
            serverUser = await db.ServerUsers.FindAsync(context.User.Id, context.Guild.Id);

        if (serverUser is null)
            return PreconditionResult.FromError($"User {context.User.GetFullName()} was not found in guild {context.Guild.Name}");


        if (serverUser.StandartAccessLevel >= _accessLevel)
            return PreconditionResult.FromSuccess();

        return PreconditionResult.FromError(ErrorMessage ?? $"User {context.User.GetFullName()} does not have the required access level **`{_accessLevel}`**");
    }
}
