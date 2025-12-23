using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class FreshStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WishList_Medicines_ApiId",
                table: "WishList");

            migrationBuilder.DropIndex(
                name: "IX_WishList_ApiId",
                table: "WishList");

            migrationBuilder.DropColumn(
                name: "ApiId",
                table: "WishList");

            migrationBuilder.CreateIndex(
                name: "IX_WishList_MedicineId",
                table: "WishList",
                column: "MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_WishList_Medicines_MedicineId",
                table: "WishList",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WishList_Medicines_MedicineId",
                table: "WishList");

            migrationBuilder.DropIndex(
                name: "IX_WishList_MedicineId",
                table: "WishList");

            migrationBuilder.AddColumn<string>(
                name: "ApiId",
                table: "WishList",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WishList_ApiId",
                table: "WishList",
                column: "ApiId");

            migrationBuilder.AddForeignKey(
                name: "FK_WishList_Medicines_ApiId",
                table: "WishList",
                column: "ApiId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
