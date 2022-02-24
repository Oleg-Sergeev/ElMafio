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
    [Migration("20220212115808_AddQuizStats")]
    partial class AddQuizStats
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<decimal?>("CategoryChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<bool>("ClearChannelsOnStart")
                        .HasColumnType("bit");

                    b.Property<int?>("CurrentTemplateId")
                        .HasColumnType("int");

                    b.Property<decimal?>("GeneralTextChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("GeneralVoiceChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GuildSettingsId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("MafiaRoleId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("MurdersTextChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("MurdersVoiceChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("SpectatorsTextChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("SpectatorsVoiceChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("WatcherRoleId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("CurrentTemplateId");

                    b.HasIndex("GuildSettingsId")
                        .IsUnique();

                    b.ToTable("MafiaSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("MafiaSettingsId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("PreGameMessage")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("MafiaSettingsId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("MafiaSettingsTemplates");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.GameSubSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<bool>("ConditionAliveAtLeast1Innocent")
                        .HasColumnType("bit");

                    b.Property<bool>("ConditionContinueGameWithNeutrals")
                        .HasColumnType("bit");

                    b.Property<bool>("IsCustomGame")
                        .HasColumnType("bit");

                    b.Property<bool>("IsFillWithMurders")
                        .HasColumnType("bit");

                    b.Property<bool>("IsRatingGame")
                        .HasColumnType("bit");

                    b.Property<int>("LastWordNightCount")
                        .HasColumnType("int");

                    b.Property<int>("MafiaCoefficient")
                        .HasColumnType("int");

                    b.Property<int>("MafiaSettingsTemplateId")
                        .HasColumnType("int");

                    b.Property<int>("VoteTime")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("MafiaSettingsTemplateId")
                        .IsUnique();

                    b.ToTable("GameSubSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.RoleAmountSubSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("DoctorsCount")
                        .HasColumnType("int");

                    b.Property<int>("DonsCount")
                        .HasColumnType("int");

                    b.Property<int>("HookersCount")
                        .HasColumnType("int");

                    b.Property<int>("InnocentsCount")
                        .HasColumnType("int");

                    b.Property<int>("MafiaSettingsTemplateId")
                        .HasColumnType("int");

                    b.Property<int>("ManiacsCount")
                        .HasColumnType("int");

                    b.Property<int>("MurdersCount")
                        .HasColumnType("int");

                    b.Property<int>("SheriffsCount")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("MafiaSettingsTemplateId")
                        .IsUnique();

                    b.ToTable("RoleAmountSubSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.RolesExtraInfoSubSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<bool>("CanInnocentsKillAtNight")
                        .HasColumnType("bit");

                    b.Property<int>("DoctorSelfHealsCount")
                        .HasColumnType("int");

                    b.Property<bool>("InnocentsMustVoteForOnePlayer")
                        .HasColumnType("bit");

                    b.Property<int>("MafiaSettingsTemplateId")
                        .HasColumnType("int");

                    b.Property<bool>("MurdersKnowEachOther")
                        .HasColumnType("bit");

                    b.Property<bool>("MurdersMustVoteForOnePlayer")
                        .HasColumnType("bit");

                    b.Property<bool>("MurdersVoteTogether")
                        .HasColumnType("bit");

                    b.Property<int>("SheriffShotsCount")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("MafiaSettingsTemplateId")
                        .IsUnique();

                    b.ToTable("RolesExtraInfoSubSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.ServerSubSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<bool>("AbortGameWhenError")
                        .HasColumnType("bit");

                    b.Property<int>("MafiaSettingsTemplateId")
                        .HasColumnType("int");

                    b.Property<bool>("MentionPlayersOnGameStart")
                        .HasColumnType("bit");

                    b.Property<bool>("RemoveRolesFromUsers")
                        .HasColumnType("bit");

                    b.Property<bool>("RenameUsers")
                        .HasColumnType("bit");

                    b.Property<bool>("ReplyMessagesOnSetupError")
                        .HasColumnType("bit");

                    b.Property<bool>("SendWelcomeMessage")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("MafiaSettingsTemplateId")
                        .IsUnique();

                    b.ToTable("ServerSubSettings");
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

                    b.Property<float>("DoctorEfficiency")
                        .HasColumnType("real");

                    b.Property<int>("DoctorHealsCount")
                        .HasColumnType("int");

                    b.Property<int>("DoctorMovesCount")
                        .HasColumnType("int");

                    b.Property<float>("DonEfficiency")
                        .HasColumnType("real");

                    b.Property<int>("DonMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("DonRevealsCount")
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

                    b.Property<float>("SheriffEfficiency")
                        .HasColumnType("real");

                    b.Property<int>("SheriffKillsCount")
                        .HasColumnType("int");

                    b.Property<int>("SheriffMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("SheriffRevealsCount")
                        .HasColumnType("int");

                    b.Property<float>("WinRate")
                        .HasColumnType("real");

                    b.Property<int>("WinsCount")
                        .HasColumnType("int");

                    b.HasKey("UserId", "GuildSettingsId");

                    b.HasIndex("GuildSettingsId");

                    b.ToTable("MafiaStats");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.QuizStats", b =>
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

                    b.ToTable("QuizStats");
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

                    b.Property<DateTime?>("JoinedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", "CurrentTemplate")
                        .WithMany()
                        .HasForeignKey("CurrentTemplateId");

                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "GuildSettings")
                        .WithOne("MafiaSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", "GuildSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CurrentTemplate");

                    b.Navigation("GuildSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettings", null)
                        .WithMany("MafiaSettingsTemplates")
                        .HasForeignKey("MafiaSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.GameSubSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", null)
                        .WithOne("GameSubSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.GameSubSettings", "MafiaSettingsTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.RoleAmountSubSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", null)
                        .WithOne("RoleAmountSubSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.RoleAmountSubSettings", "MafiaSettingsTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.RolesExtraInfoSubSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", null)
                        .WithOne("RolesExtraInfoSubSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.RolesExtraInfoSubSettings", "MafiaSettingsTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.ServerSubSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", null)
                        .WithOne("ServerSubSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings.ServerSubSettings", "MafiaSettingsTemplateId")
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

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.QuizStats", b =>
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
                    b.Navigation("MafiaSettingsTemplates");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.Mafia.MafiaSettingsTemplate", b =>
                {
                    b.Navigation("GameSubSettings")
                        .IsRequired();

                    b.Navigation("RoleAmountSubSettings")
                        .IsRequired();

                    b.Navigation("RolesExtraInfoSubSettings")
                        .IsRequired();

                    b.Navigation("ServerSubSettings")
                        .IsRequired();
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