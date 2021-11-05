﻿// <auto-generated />
using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Migrations
{
    [DbContext(typeof(BotContext))]
    [Migration("20211104175319_TableMafiaSettingsAddCategoryChannelId")]
    partial class TableMafiaSettingsAddCategoryChannelId
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.11")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.MafiaSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("AbortGameWhenError")
                        .HasColumnType("bit");

                    b.Property<decimal?>("CategoryChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("GeneralTextChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("GeneralVoiceChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("GuildSettingsId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<bool>("IsRatingGame")
                        .HasColumnType("bit");

                    b.Property<int>("MafiaKoefficient")
                        .HasColumnType("int");

                    b.Property<decimal?>("MafiaRoleId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("MurdersTextChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("MurdersVoiceChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<bool>("RenameUsers")
                        .HasColumnType("bit");

                    b.Property<bool>("ReplyMessagesOnError")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<decimal?>("WatcherRoleId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildSettingsId")
                        .IsUnique();

                    b.ToTable("MafiaSettings");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.RussianRouletteSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int>("CommissionerMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("CommissionerSuccessfullMovesCount")
                        .HasColumnType("int");

                    b.Property<float>("CommissionerWinRate")
                        .HasColumnType("real");

                    b.Property<int>("DoctorMovesCount")
                        .HasColumnType("int");

                    b.Property<int>("DoctorSuccessfullMovesCount")
                        .HasColumnType("int");

                    b.Property<float>("DoctorWinRate")
                        .HasColumnType("real");

                    b.Property<float>("ExtraScores")
                        .HasColumnType("real");

                    b.Property<int>("GamesCount")
                        .HasColumnType("int");

                    b.Property<int>("MurderGamesCount")
                        .HasColumnType("int");

                    b.Property<float>("MurderWinRate")
                        .HasColumnType("real");

                    b.Property<int>("MurderWinsCount")
                        .HasColumnType("int");

                    b.Property<float>("Rating")
                        .HasColumnType("real");

                    b.Property<float>("Scores")
                        .HasColumnType("real");

                    b.Property<float>("TotalWinRate")
                        .HasColumnType("real");

                    b.Property<float>("WinRate")
                        .HasColumnType("real");

                    b.Property<int>("WinsCount")
                        .HasColumnType("int");

                    b.HasKey("UserId", "GuildId");

                    b.HasIndex("GuildId");

                    b.ToTable("MafiaStats");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.RussianRouletteStats", b =>
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

            modelBuilder.Entity("Infrastructure.Data.Models.GuildSettings", b =>
                {
                    b.Property<decimal>("Id")
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

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Settings.MafiaSettings", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "GuildSettings")
                        .WithOne("MafiaSettings")
                        .HasForeignKey("Infrastructure.Data.Models.Games.Settings.MafiaSettings", "GuildSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildSettings");
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
                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Infrastructure.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Infrastructure.Data.Models.Games.Stats.RussianRouletteStats", b =>
                {
                    b.HasOne("Infrastructure.Data.Models.GuildSettings", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Infrastructure.Data.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("User");
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
