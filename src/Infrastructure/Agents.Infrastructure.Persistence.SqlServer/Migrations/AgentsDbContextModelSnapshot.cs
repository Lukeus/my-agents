using System;
using Agents.Infrastructure.Persistence.SqlServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Agents.Infrastructure.Persistence.SqlServer.Migrations
{
    [DbContext(typeof(AgentsDbContext))]
    partial class AgentsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Agents.Domain.Notification.Entities.Notification", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Channel")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(10000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("DeliveredAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("ErrorMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<string>("FormattedContent")
                        .HasMaxLength(10000)
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Recipient")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<int>("RetryCount")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("SentAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("Id");

                    b.HasIndex("Channel");

                    b.HasIndex("SentAt");

                    b.HasIndex("Status");

                    b.ToTable("Notifications", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
