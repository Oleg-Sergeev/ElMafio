using System;
using System.Threading.Tasks;
using Discord.Commands;
using Infrastructure.Data.ViewModels;

namespace Infrastructure.TypeReaders;

public class MafiaSettingsTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string? input, IServiceProvider services)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "The input is empty"));

        var args = input.Split();

        if (args.Length < 6)
            return Task.FromResult(TypeReaderResult.FromError(CommandError.BadArgCount, "Too few parameters"));


        if (!int.TryParse(args[0], out var mafiaKoefficient))
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Failed to parse field {nameof(MafiaSettingsViewModel.MafiaKoefficient)}"));


        var boolTypeReader = new BooleanTypeReader();
        var fields = new string[]
        {
                nameof(MafiaSettingsViewModel.IsRatingGame),
                nameof(MafiaSettingsViewModel.RenameUsers),
                nameof(MafiaSettingsViewModel.ReplyMessagesOnSetupError),
                nameof(MafiaSettingsViewModel.AbortGameWhenError),
                nameof(MafiaSettingsViewModel.SendWelcomeMessage),
        };
        var values = new bool?[5];

        for (int i = 0; i < values.Length; i++)
        {
            var res = boolTypeReader.ReadAsync(context, args[i + 1], services).GetAwaiter().GetResult();

            if (!res.IsSuccess)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Failed to parse field {fields[i]}"));

            values[i] = res.Values is not null ? (bool)res.BestMatch : null;
        }


        var mafiaSettings = new MafiaSettingsViewModel
        {
            MafiaKoefficient = mafiaKoefficient,
            IsRatingGame = values[0],
            RenameUsers = values[1],
            ReplyMessagesOnSetupError = values[2],
            AbortGameWhenError = values[3],
            SendWelcomeMessage = values[4]
        };

        return Task.FromResult(TypeReaderResult.FromSuccess(mafiaSettings));
    }
}