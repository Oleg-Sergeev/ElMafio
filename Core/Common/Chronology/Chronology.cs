using System.Collections;

namespace Core.Common.Chronology;

public class Chronology<T> : IEnumerable<T>
{
    protected List<T> Actions { get; }

    public Chronology()
    {
        Actions = new();
    }


    public void AddAction(T action) => Actions.Add(action);


    public IReadOnlyList<T> GetActionsHistory() => Actions;


    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Actions.Count; i++)
            yield return Actions[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}