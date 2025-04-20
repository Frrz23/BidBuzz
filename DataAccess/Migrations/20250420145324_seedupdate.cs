using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class seedupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Auctions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.InsertData(
                table: "AuctionSchedules",
                columns: new[] { "Id", "EndDay", "EndHour", "StartDay", "StartHour", "Week" },
                values: new object[] { 2, "Sunday", 1, "Saturday", 12, "Next" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AuctionSchedules",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.InsertData(
                table: "Auctions",
                columns: new[] { "Id", "EndTime", "ItemId", "StartTime", "Status" },
                values: new object[] { 1, new DateTime(2025, 2, 25, 12, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2025, 2, 24, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 });
        }
    }
}
