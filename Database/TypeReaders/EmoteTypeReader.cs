using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Infrastructure.TypeReaders;

public class EmoteTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string? input, IServiceProvider services)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "The input is empty"));

        if (!Emote.TryParse(input, out var emote))
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input cannot be parsed as Emote"));


        return Task.FromResult(TypeReaderResult.FromSuccess(emote));
    }
}