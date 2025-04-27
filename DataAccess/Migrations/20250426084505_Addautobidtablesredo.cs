using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Addautobidtablesredo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoBids_AspNetUsers_UserId",
                table: "AutoBids");

            migrationBuilder.RenameColumn(
                name: "MaxBidAmount",
                table: "AutoBids",
                newName: "MaxAmount");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoBids_AspNetUsers_UserId",
                table: "AutoBids",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoBids_AspNetUsers_UserId",
                table: "AutoBids");

            migrationBuilder.RenameColumn(
                name: "MaxAmount",
                table: "AutoBids",
                newName: "MaxBidAmount");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoBids_AspNetUsers_UserId",
                table: "AutoBids",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
