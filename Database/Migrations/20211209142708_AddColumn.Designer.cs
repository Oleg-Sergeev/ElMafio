﻿// <auto-generated />
using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(BotContext))]
    [Migration("20211209142708_AddColumn")]
    partial class AddColumn
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("CurrentTemplateName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("GuildSettingsId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildSettingsId")
                        .IsUnique();

                    b.ToTable("MafiaSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SettingsTemplate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("GameSubSettingsJsonData")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GuildSubSettingsJsonData")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("MafiaSettingsId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PreGameMessage")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleAmountSubSettingsJsonData")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RolesInfoSubSettingsJsonData")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("MafiaSettingsId");

                    b.ToTable("MafiaSettingsTemplates");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.RussianRouletteSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("CustomSmileKilled")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CustomSmileSurvived")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("GuildSettingsId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("UnicodeSmileKilled")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("💀");

                    b.Property<string>("UnicodeSmileSurvived")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("😎");

                    b.HasKey("Id");

                    b.HasIndex("GuildSettingsId")
                        .IsUnique();

                    b.ToTable("RussianRouletteSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.MafiaStats", b =>
                {
                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GuildSettingsId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int>("BlacksGamesCount")
                        .HasColumnType("int");

                    b.Property<float>("BlacksWinRate")
                        .HasColumnType("real");

                    b.Property<int>("BlacksWinsCount")
                        .HasColumnType("int");

                    b.Property<float>("CommissionerEfficiency")
                        .HasColumnType("real");

                    b.Property<int>("CommissionerMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("CommissionerSuccessfullFoundsCount")
                        .HasColumnType("int");

                    b.Property<int>("CommissionerSuccessfullShotsCount")
                        .HasColumnType("int");

                    b.Property<float>("DoctorEfficiency")
                        .HasColumnType("real");

                    b.Property<int>("DoctorMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("DoctorSuccessfullMovesCount")
                        .HasColumnType("int");

                    b.Property<float>("DonEfficiency")
                        .HasColumnType("real");

                    b.Property<int>("DonMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("DonSuccessfullMovesCount")
                        .HasColumnType("int");

                    b.Property<float>("ExtraScores")
                        .HasColumnType("real");

                    b.Property<int>("GamesCount")
                        .HasColumnType("int");

                    b.Property<float>("PenaltyScores")
                        .HasColumnType("real");

                    b.Property<float>("Rating")
                        .HasColumnType("real");

                    b.Property<float>("Scores")
                        .HasColumnType("real");

                    b.Property<float>("WinRate")
                        .HasColumnType("real");

                    b.Property<int>("WinsCount")
                        .HasColumnType("int");

                    b.HasKey("UserId", "GuildSettingsId");

                    b.HasIndex("GuildSettingsId");

                    b.ToTable("MafiaStats");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.RussianRouletteStats", b =>
                {
                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GuildSettingsId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int>("GamesCount")
                        .HasColumnType("int");

                    b.Property<float>("WinRate")
                        .HasColumnType("real");

                    b.Property<int>("WinsCount")
                        .HasColumnType("int");

                    b.HasKey("UserId", "GuildSettingsId");

                    b.HasIndex("GuildSettingsId");

                    b.ToTable("RussianRouletteStats");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.GuildSettings", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("LogChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("Prefix")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("/");

                    b.Property<decimal?>("RoleMuteId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.ToTable("GuildSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.User", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime>("JoinedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "GuildSettings")
                        .WithOne("MafiaSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", "GuildSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SettingsTemplate", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", null)
                        .WithMany("SettingsTemplates")
                        .HasForeignKey("MafiaSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.RussianRouletteSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "GuildSettings")
                        .WithOne("RussianRouletteSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.RussianRouletteSettings", "GuildSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.MafiaStats", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "GuildSettings")
                        .WithMany()
                        .HasForeignKey("GuildSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Infrastructure.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildSettings");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.RussianRouletteStats", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "GuildSettings")
                        .WithMany()
                        .HasForeignKey("GuildSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Infrastructure.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildSettings");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", b =>
                {
                    b.Navigation("SettingsTemplates");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.GuildSettings", b =>
                {
                    b.Navigation("MafiaSettings")
                        .IsRequired();

                    b.Navigation("RussianRouletteSettings")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
