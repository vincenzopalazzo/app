﻿// <auto-generated />
using System;
using BTCPayApp.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BTCPayApp.Core.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240502121610_Init")]
    partial class Init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("BTCPayApp.Core.Data.Channel", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("Aliases")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.ToTable("LightningChannels");
                });

            modelBuilder.Entity("BTCPayApp.Core.Data.LightningPayment", b =>
                {
                    b.Property<string>("PaymentHash")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Inbound")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PaymentId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Preimage")
                        .HasColumnType("TEXT");

                    b.Property<string>("Secret")
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<long>("Value")
                        .HasColumnType("INTEGER");

                    b.HasKey("PaymentHash", "Inbound");

                    b.ToTable("LightningPayments");
                });

            modelBuilder.Entity("BTCPayApp.Core.Data.Setting", b =>
                {
                    b.Property<string>("Key")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Value")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.HasKey("Key");

                    b.ToTable("Settings");
                });
#pragma warning restore 612, 618
        }
    }
}
