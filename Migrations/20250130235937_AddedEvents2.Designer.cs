﻿// <auto-generated />
using System;
using BotTemplate.BotCore.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BotTemplate.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20250130235937_AddedEvents2")]
    partial class AddedEvents2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("BotTemplate.BotCore.Entities.BoughtWeapon", b =>
                {
                    b.Property<int>("BoughtWeaponId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("BoughtWeaponId"));

                    b.Property<int>("Amount")
                        .HasColumnType("int");

                    b.Property<DateTime>("Delivered")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Ordered")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("WeaponId")
                        .HasColumnType("int");

                    b.HasKey("BoughtWeaponId");

                    b.HasIndex("UserId");

                    b.HasIndex("WeaponId");

                    b.ToTable("BoughtWeapons");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Event", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("EventId"));

                    b.Property<DateTime>("EventDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("EventDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EventLocation")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EventTitle")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("EventType")
                        .HasColumnType("int");

                    b.Property<int>("MadeByUserId")
                        .HasColumnType("int");

                    b.HasKey("EventId");

                    b.HasIndex("MadeByUserId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Note", b =>
                {
                    b.Property<int>("NoteId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("NoteId"));

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.HasKey("NoteId");

                    b.HasIndex("CreatedByUserId");

                    b.ToTable("Notes");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Strike", b =>
                {
                    b.Property<int>("StrikeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("StrikeId"));

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<int>("GivenByUserId")
                        .HasColumnType("int");

                    b.Property<int>("GivenToUserId")
                        .HasColumnType("int");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("StrikeId");

                    b.HasIndex("GivenByUserId");

                    b.HasIndex("GivenToUserId");

                    b.ToTable("Strikes");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"));

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int?>("EventId")
                        .HasColumnType("int");

                    b.Property<int?>("EventId1")
                        .HasColumnType("int");

                    b.Property<string>("IngameName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("JoinDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.HasKey("UserId");

                    b.HasIndex("EventId");

                    b.HasIndex("EventId1");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Weapon", b =>
                {
                    b.Property<int>("WeaponId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("WeaponId"));

                    b.Property<int>("WeaponLimit")
                        .HasColumnType("int");

                    b.Property<string>("WeaponName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("WeaponPrice")
                        .HasColumnType("int");

                    b.HasKey("WeaponId");

                    b.ToTable("Weapons");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.BoughtWeapon", b =>
                {
                    b.HasOne("BotTemplate.BotCore.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BotTemplate.BotCore.Entities.Weapon", "Weapon")
                        .WithMany()
                        .HasForeignKey("WeaponId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");

                    b.Navigation("Weapon");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Event", b =>
                {
                    b.HasOne("BotTemplate.BotCore.Entities.User", "MadeBy")
                        .WithMany()
                        .HasForeignKey("MadeByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MadeBy");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Note", b =>
                {
                    b.HasOne("BotTemplate.BotCore.Entities.User", "CreatedBy")
                        .WithMany("Notes")
                        .HasForeignKey("CreatedByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CreatedBy");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Strike", b =>
                {
                    b.HasOne("BotTemplate.BotCore.Entities.User", "GivenBy")
                        .WithMany()
                        .HasForeignKey("GivenByUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("BotTemplate.BotCore.Entities.User", "GivenTo")
                        .WithMany()
                        .HasForeignKey("GivenToUserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("GivenBy");

                    b.Navigation("GivenTo");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.User", b =>
                {
                    b.HasOne("BotTemplate.BotCore.Entities.Event", null)
                        .WithMany("Absent")
                        .HasForeignKey("EventId");

                    b.HasOne("BotTemplate.BotCore.Entities.Event", null)
                        .WithMany("Participants")
                        .HasForeignKey("EventId1");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Event", b =>
                {
                    b.Navigation("Absent");

                    b.Navigation("Participants");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.User", b =>
                {
                    b.Navigation("Notes");
                });
#pragma warning restore 612, 618
        }
    }
}
