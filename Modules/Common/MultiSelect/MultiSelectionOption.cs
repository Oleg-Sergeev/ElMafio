namespace Modules.Common.MultiSelect;

public class MultiSelectionOption<T> where T : notnull
{
    public MultiSelectionOption(T option, int row, bool isDefault = false, string? description = null)
    {
        Option = option;
        Row = row;
        IsDefault = isDefault;
        Description = description;
    }

    public T Option { get; }

    public string? Description { get; }

    public int Row { get; }

    public bool IsDefault { get; }

    public override string? ToString() => Option.ToString();

    public override int GetHashCode() => Option.GetHashCode();

    public override bool Equals(object? obj) => Option.Equals(obj);
}
