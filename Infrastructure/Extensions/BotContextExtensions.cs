using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data;
using Infrastructure.Data.Models;

namespace Infrastructure.Extensions;

public static class BotContextExtensions
{
    public static void SeedDatabase(this BotContext context)
    {
        if (context.Users.Any())
            return;


        context.SeedUsers();
    }

    private static void SeedUsers(this BotContext context)
    {
        var users = GetUsers();

        context.Users.AddRange(users);
    }


    private static List<User> GetUsers()
        => new()
        {
            new()
            {
                Id = 184316176007036928,
                JoinedAt = new DateTime(2020, 10, 29)
            },

            new()
            {
                Id = 795322165795880960,
                JoinedAt = new DateTime(2021, 1, 3)
            },

            new()
            {
                Id = 903248920099057744,
                JoinedAt = new DateTime(2021, 10, 29)
            }
        };
}
