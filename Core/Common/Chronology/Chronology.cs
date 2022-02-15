using System.Collections;

namespace Core.Common.Chronology;

public class Chronology<T>
{
    protected List<T> Actions { get; }

    public Chronology()
    {
        Actions = new();
    }


    public IReadOnlyList<T> GetActionHistory() => Actions;


    public void AddAction(T action) => Actions.Add(action);
}