namespace Modules.Extensions;

public static class EmojiExtensions
{
    public static string ConvertToSmile(this int num) => num switch
    {
        1 => "1️⃣",
        2 => "2️⃣",
        3 => "3️⃣",
        4 => "4️⃣",
        5 => "5️⃣",
        6 => "6️⃣",
        7 => "7️⃣",
        8 => "8️⃣",
        9 => "9️⃣",

        _ => "0️⃣"
    };
    public static string? ConvertToSmile(this char letter)
    {
        var smile = letter switch
        {
            'ч' or 'Ч' => GetSmile('c') + '\u202F' + GetSmile('h'),
            'ш' or 'Ш' => GetSmile('s') + '\u202F' + GetSmile('h'),
            'щ' or 'Щ' => GetSmile('s') + '\u202F' + GetSmile('c') + '\u202F' + GetSmile('h'),
            'ю' or 'Ю' => GetSmile('y') + '\u202F' + GetSmile('u'),
            'я' or 'Я' => GetSmile('y') + '\u202F' + GetSmile('a'),

            _ => GetSmile(letter)
        };

        return smile;


        static string? GetSmile(char letter) => letter switch
        {
            'a' or 'A' or 'а' or 'А' => "\uD83C\uDDE6",
            'b' or 'B' or 'б' or 'Б' => "\uD83C\uDDE7",
            'c' or 'C' or 'ц' or 'Ц' => "\uD83C\uDDE8",
            'd' or 'D' or 'д' or 'Д' => "\uD83C\uDDE9",
            'e' or 'E' or 'е' or 'Е' => "\uD83C\uDDEA",
            'f' or 'F' or 'ф' or 'Ф' => "\uD83C\uDDEB",
            'g' or 'G' or 'г' or 'Г' => "\uD83C\uDDEC",
            'h' or 'H' or 'х' or 'Х' => "\uD83C\uDDED",
            'i' or 'I' or 'и' or 'И' or 'й' or 'Й' => "\uD83C\uDDEE",
            'j' or 'J' or 'ж' or 'Ж' => "\uD83C\uDDEF",
            'k' or 'K' or 'к' or 'К' => "\uD83C\uDDF0",
            'l' or 'L' or 'л' or 'Л' => "\uD83C\uDDF1",
            'm' or 'M' or 'м' or 'М' => "\uD83C\uDDF2",
            'n' or 'N' or 'н' or 'Н' => "\uD83C\uDDF3",
            'o' or 'O' or 'о' or 'О' => "\uD83C\uDDF4",
            'p' or 'P' or 'п' or 'П' => "\uD83C\uDDF5",
            'q' or 'Q' => "\uD83C\uDDF6",
            'r' or 'R' or 'р' or 'Р' => "\uD83C\uDDF7",
            's' or 'S' or 'с' or 'С' => "\uD83C\uDDF8",
            't' or 'T' or 'т' or 'Т' => "\uD83C\uDDF9",
            'u' or 'U' or 'у' or 'У' => "\uD83C\uDDFA",
            'v' or 'V' or 'в' or 'В' => "\uD83C\uDDFB",
            'w' or 'W' => "\uD83C\uDDFC",
            'x' or 'X' => "\uD83C\uDDFD",
            'y' or 'Y' or 'ы' or 'Ы' => "\uD83C\uDDFE",
            'z' or 'Z' or 'з' or 'З' => "\uD83C\uDDFF",

            { } l when l >= '0' && l <= '9' => (l - '0').ConvertToSmile(),

            _ => null
        };
    }

    public static bool TryConvertToSmile(this char letter, out string? str)
    {
        var smile = letter.ConvertToSmile();

        str = smile;

        return !string.IsNullOrWhiteSpace(str);
    }

}
