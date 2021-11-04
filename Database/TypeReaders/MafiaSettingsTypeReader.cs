using System;
using System.Threading.Tasks;
using Discord.Commands;
using Infrastructure.ViewModels;

namespace Infrastructure.TypeReaders
{
    public class MafiaSettingsTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string? input, IServiceProvider services)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "The input is empty"));

            var args = input.Split();

            if (args.Length < 2)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.BadArgCount, "Too few parameters"));

            if (!int.TryParse(args[0], out var mafiaKoefficient))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Failed parse field {nameof(MafiaSettingsViewModel.MafiaKoefficient)}"));

            var boolTypeReader = new BooleanTypeReader();

            var res = boolTypeReader.ReadAsync(context, args[1], services).GetAwaiter().GetResult();

            if (!res.IsSuccess)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Failed parse field {nameof(MafiaSettingsViewModel.IsRatingGame)}"));

            if (res.BestMatch is not bool isRatingGame)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Failed parse field {nameof(MafiaSettingsViewModel.IsRatingGame)}"));


            var mafiaSettings = new MafiaSettingsViewModel(mafiaKoefficient, isRatingGame);

            return Task.FromResult(TypeReaderResult.FromSuccess(mafiaSettings));
        }
    }
}
