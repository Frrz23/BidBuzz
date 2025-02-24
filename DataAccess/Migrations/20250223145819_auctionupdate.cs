using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class auctionupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Auctions",
                columns: new[] { "Id", "EndTime", "ItemId", "StartTime", "Status" },
                values: new object[] { 1, new DateTime(2025, 2, 25, 12, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2025, 2, 24, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Auctions",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
