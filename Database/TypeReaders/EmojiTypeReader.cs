using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Infrastructure.TypeReaders;

public class EmojiTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string? input, IServiceProvider services)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "The input is empty"));

        if (input.Any(c => char.IsLetterOrDigit(c) || char.IsSymbol(c)))
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input cannot be parsed as Emoji"));


        return Task.FromResult(TypeReaderResult.FromSuccess(new Emoji(input)));
    }
}
