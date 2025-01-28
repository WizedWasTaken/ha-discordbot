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
    [Migration("20250128050919_AddedStrikes")]
    partial class AddedStrikes
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

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

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("StrikeId");

                    b.HasIndex("GivenByUserId");

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

                    b.Property<string>("IngameName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("JoinDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.Strike", b =>
                {
                    b.HasOne("BotTemplate.BotCore.Entities.User", "GivenBy")
                        .WithMany("Strikes")
                        .HasForeignKey("GivenByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GivenBy");
                });

            modelBuilder.Entity("BotTemplate.BotCore.Entities.User", b =>
                {
                    b.Navigation("Strikes");
                });
#pragma warning restore 612, 618
        }
    }
}
