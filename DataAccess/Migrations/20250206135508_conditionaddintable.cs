using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class conditionaddintable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auctions_Bids_WinningBidId",
                table: "Auctions");

            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Items_ItemId",
                table: "Bids");

            migrationBuilder.DropIndex(
                name: "IX_Auctions_ItemId",
                table: "Auctions");

            migrationBuilder.DropIndex(
                name: "IX_Auctions_WinningBidId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "WinningBidId",
                table: "Auctions");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "Bids",
                newName: "AuctionId");

            migrationBuilder.RenameIndex(
                name: "IX_Bids_ItemId",
                table: "Bids",
                newName: "IX_Bids_AuctionId");

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_ItemId",
                table: "Auctions",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Auctions_AuctionId",
                table: "Bids",
                column: "AuctionId",
                principalTable: "Auctions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Auctions_AuctionId",
                table: "Bids");

            migrationBuilder.DropIndex(
                name: "IX_Auctions_ItemId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "AuctionId",
                table: "Bids",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Bids_AuctionId",
                table: "Bids",
                newName: "IX_Bids_ItemId");

            migrationBuilder.AddColumn<int>(
                name: "WinningBidId",
                table: "Auctions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_ItemId",
                table: "Auctions",
                column: "ItemId",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Items_ItemId",
                table: "Bids",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
