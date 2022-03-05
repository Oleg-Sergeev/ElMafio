using Discord;

namespace Core.Extensions;

public static class OverwritePermissionsExtensions
{
    public static bool AreSame(this OverwritePermissions o1, OverwritePermissions? o2)
        => o1.AllowValue == o2?.AllowValue && o1.DenyValue == o2?.DenyValue;

    public static bool AreSame(this OverwritePermissions? o1, OverwritePermissions? o2)
        => o1 is not null && o1.Value.AreSame(o2);
}
