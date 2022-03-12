using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Entities;

public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public DateTime? JoinedAt { get; set; }
}