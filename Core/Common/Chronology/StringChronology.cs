namespace Core.Common.Chronology;


public class StringChronology : Chronology<string>
{
    public string FlattenActionsHistory()
    {
        var str = string.Join(Environment.NewLine, Actions);

        return str;
    }
}
