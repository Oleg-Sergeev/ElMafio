﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Database.Migrations
{
    [DbContext(typeof(BotContext))]
    [Migration("20211031191317_Init")]
    partial class Init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.11")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Database.Data.Models.Guild", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("Database.Data.Models.MafiaStats", b =>
                {
                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int>("CommissionerMovesCount")
                        .HasColumnType("int");

                    b.Property<float>("CommissionerRating")
                        .HasColumnType("real");

                    b.Property<int>("CommissionerSuccessfullMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("DoctorMovesCount")
                        .HasColumnType("int");

                    b.Property<float>("DoctorRating")
                        .HasColumnType("real");

                    b.Property<int>("DoctorSuccessfullMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("GamesCount")
                        .HasColumnType("int");

                    b.Property<int>("MurderGamesCount")
                        .HasColumnType("int");

                    b.Property<float>("MurderRating")
                        .HasColumnType("real");

                    b.Property<int>("MurderWinsCount")
                        .HasColumnType("int");

                    b.Property<float>("TotalRating")
                        .HasColumnType("real");

                    b.Property<float>("WinRate")
                        .HasColumnType("real");

                    b.Property<int>("WinsCount")
                        .HasColumnType("int");

                    b.HasKey("UserId", "GuildId");

                    b.HasIndex("GuildId");

                    b.ToTable("MafiaStats");
                });

            modelBuilder.Entity("Database.Data.Models.RussianRouletteStats", b =>
                {
                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int>("GamesCount")
                        .HasColumnType("int");

                    b.Property<float>("WinRate")
                        .HasColumnType("real");

                    b.Property<int>("WinsCount")
                        .HasColumnType("int");

                    b.HasKey("UserId", "GuildId");

                    b.HasIndex("GuildId");

                    b.ToTable("RussianRouletteStats");
                });

            modelBuilder.Entity("Database.Data.Models.User", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime>("JoinedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Database.Data.Models.MafiaStats", b =>
                {
                    b.HasOne("Database.Data.Models.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Database.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Database.Data.Models.RussianRouletteStats", b =>
                {
                    b.HasOne("Database.Data.Models.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Database.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}
