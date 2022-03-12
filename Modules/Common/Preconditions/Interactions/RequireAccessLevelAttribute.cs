using System;
using System.Threading.Tasks;
using Core.Extensions;
using Discord;
using Discord.Interactions;
using Infrastructure.Data;
using Infrastructure.Data.Entities;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Common.Preconditions.Interactions;


public class RequireStandartAccessLevelAttribute : PreconditionAttribute
{
    private readonly StandartAccessLevel _accessLevel;


    public RequireStandartAccessLevelAttribute(StandartAccessLevel accessLevel)
    {
        _accessLevel = accessLevel;
    }


    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        if (context.Guild is null || context.Interaction.Type == InteractionType.MessageComponent)
            return PreconditionResult.FromSuccess();

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
