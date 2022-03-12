using System.Collections.Generic;

namespace Infrastructure.Data.Entities;

public class AccessLevel
{
    public int Id { get; set; }

    public ulong ServerId { get; set; }


    public string Name { get; set; }

    public int Priority { get; set; }


    public AccessLevel(string name)
    {
        Name = name;
    }
}
