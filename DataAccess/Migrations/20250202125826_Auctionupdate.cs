using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Auctionupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Auctions");

            migrationBuilder.RenameColumn(
                name: "IsApproved",
                table: "Items",
                newName: "IsApprovedForAuction");

            migrationBuilder.AddColumn<DateTime>(
                name: "AuctionEndTime",
                table: "Items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuctionStartTime",
                table: "Items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Auctions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WinningBidId",
                table: "Auctions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_WinningBidId",
                table: "Auctions",
                column: "WinningBidId");

            migrationBuilder.AddForeignKey(
                name: "FK_Auctions_Bids_WinningBidId",
                table: "Auctions",
                column: "WinningBidId",
                principalTable: "Bids",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auctions_Bids_WinningBidId",
                table: "Auctions");

            migrationBuilder.DropIndex(
                name: "IX_Auctions_WinningBidId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "AuctionEndTime",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AuctionStartTime",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "WinningBidId",
                table: "Auctions");

            migrationBuilder.RenameColumn(
                name: "IsApprovedForAuction",
                table: "Items",
                newName: "IsApproved");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Auctions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
