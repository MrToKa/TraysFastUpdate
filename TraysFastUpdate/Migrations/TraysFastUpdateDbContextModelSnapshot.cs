﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TraysFastUpdate.Data;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    [DbContext(typeof(TraysFastUpdateDbContext))]
    partial class TraysFastUpdateDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TraysFastUpdate.Models.Cable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CableTypeId")
                        .HasColumnType("integer");

                    b.Property<string>("FromLocation")
                        .HasColumnType("text");

                    b.Property<string>("Routing")
                        .HasColumnType("text");

                    b.Property<string>("Tag")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ToLocation")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CableTypeId");

                    b.ToTable("Cables", (string)null);
                });

            modelBuilder.Entity("TraysFastUpdate.Models.CableType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double>("Diameter")
                        .HasColumnType("double precision");

                    b.Property<string>("Purpose")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("Weight")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.ToTable("CableTypes", (string)null);
                });

            modelBuilder.Entity("TraysFastUpdate.Models.Tray", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double?>("CablesWeightLoad")
                        .HasColumnType("double precision");

                    b.Property<double?>("CablesWeightPerMeter")
                        .HasColumnType("double precision");

                    b.Property<double>("Height")
                        .HasColumnType("double precision");

                    b.Property<double>("Length")
                        .HasColumnType("double precision");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Purpose")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ResultCablesWeightLoad")
                        .HasColumnType("text");

                    b.Property<string>("ResultCablesWeightPerMeter")
                        .HasColumnType("text");

                    b.Property<string>("ResultSpaceAvailable")
                        .HasColumnType("text");

                    b.Property<string>("ResultSpaceOccupied")
                        .HasColumnType("text");

                    b.Property<string>("ResultSupportsCount")
                        .HasColumnType("text");

                    b.Property<string>("ResultSupportsTotalWeight")
                        .HasColumnType("text");

                    b.Property<string>("ResultSupportsWeightLoadPerMeter")
                        .HasColumnType("text");

                    b.Property<string>("ResultTotalWeightLoad")
                        .HasColumnType("text");

                    b.Property<string>("ResultTotalWeightLoadPerMeter")
                        .HasColumnType("text");

                    b.Property<string>("ResultTrayOwnWeightLoad")
                        .HasColumnType("text");

                    b.Property<string>("ResultTrayWeightLoadPerMeter")
                        .HasColumnType("text");

                    b.Property<double?>("SpaceAvailable")
                        .HasColumnType("double precision");

                    b.Property<double?>("SpaceOccupied")
                        .HasColumnType("double precision");

                    b.Property<int?>("SupportsCount")
                        .HasColumnType("integer");

                    b.Property<double?>("SupportsTotalWeight")
                        .HasColumnType("double precision");

                    b.Property<double?>("SupportsWeightLoadPerMeter")
                        .HasColumnType("double precision");

                    b.Property<double?>("TotalWeightLoad")
                        .HasColumnType("double precision");

                    b.Property<double?>("TotalWeightLoadPerMeter")
                        .HasColumnType("double precision");

                    b.Property<double?>("TrayOwnWeightLoad")
                        .HasColumnType("double precision");

                    b.Property<double?>("TrayWeightLoadPerMeter")
                        .HasColumnType("double precision");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("Weight")
                        .HasColumnType("double precision");

                    b.Property<double>("Width")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.ToTable("Trays", (string)null);
                });

            modelBuilder.Entity("TraysFastUpdate.Models.Cable", b =>
                {
                    b.HasOne("TraysFastUpdate.Models.CableType", "CableType")
                        .WithMany("Cables")
                        .HasForeignKey("CableTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CableType");
                });

            modelBuilder.Entity("TraysFastUpdate.Models.CableType", b =>
                {
                    b.Navigation("Cables");
                });
#pragma warning restore 612, 618
        }
    }
}
