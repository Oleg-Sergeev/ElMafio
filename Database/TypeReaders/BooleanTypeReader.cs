using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Infrastructure.TypeReaders;

public class BooleanTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string? input, IServiceProvider? services = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "The input is empty"));

        if (bool.TryParse(input, out var res))
            return Task.FromResult(TypeReaderResult.FromSuccess(res));

        if (input.ToLower() == "да" || input.ToLower() == "yes" || input.ToLower() == "y" || input[0] == '+')
            return Task.FromResult(TypeReaderResult.FromSuccess(true));

        if (input.ToLower() == "нет" || input.ToLower() == "no" || input.ToLower() == "n" || input[0] == '-')
            return Task.FromResult(TypeReaderResult.FromSuccess(false));

        if (input.ToLower() == "x" || input.ToLower() == "х" || input.ToLower() == "=")
            return Task.FromResult(TypeReaderResult.FromSuccess(null));


        return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Input cannot be parsed as Boolean"));
    }
}