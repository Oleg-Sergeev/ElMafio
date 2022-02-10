using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Core.TypeReaders;

public class ColorTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string? input, IServiceProvider services)
    {
        Color? color = null;

        if (input is null)
            return Task.FromResult(TypeReaderResult.FromError(CommandError.BadArgCount, "Input is empty"));

        input = Regex.Replace(input, "[<>()#]", string.Empty, RegexOptions.Compiled);

        var arr = input.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (arr.Length == 3)
        {
            if (int.TryParse(arr[0], out var r) && int.TryParse(arr[1], out var g) && int.TryParse(arr[2], out var b))
                color = new Color(r, g, b);
            else if (float.TryParse(arr[0], out var rf) && float.TryParse(arr[1], out var gf) && float.TryParse(arr[2], out var bf))
                color = new Color(rf, gf, bf);
        }
        else
        {
            if (uint.TryParse(input, out var rawColor))
                color = new Color(rawColor);
            else if (uint.TryParse(input, NumberStyles.HexNumber, null, out rawColor))
                color = new Color(rawColor);
        }

        if (color is not null)
            return Task.FromResult(TypeReaderResult.FromSuccess(color));


        return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input cannot be parsed as Color"));
    }
}
