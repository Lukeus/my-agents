using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agents.Infrastructure.Persistence.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    FormattedContent = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Channel",
                table: "Notifications",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SentAt",
                table: "Notifications",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status",
                table: "Notifications",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
