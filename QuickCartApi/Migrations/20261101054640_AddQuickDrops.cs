using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickCartApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickDrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuickDropId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuickDrops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Sold = table.Column<int>(type: "int", nullable: false),
                    MaxPerOrder = table.Column<int>(type: "int", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickDrops", x => x.Id);
                    table.CheckConstraint("CK_QuickDrops_Sold", "[Sold] >= 0 AND [Sold] <= [Quantity]");
                    table.ForeignKey(
                        name: "FK_QuickDrops_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuickDropStep",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuickDropId = table.Column<int>(type: "int", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickDropStep", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickDropStep_QuickDrops_QuickDropId",
                        column: x => x.QuickDropId,
                        principalTable: "QuickDrops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_QuickDropId",
                table: "OrderItems",
                column: "QuickDropId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickDrops_ProductId_StartsAtUtc",
                table: "QuickDrops",
                columns: new[] { "ProductId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_QuickDropStep_QuickDropId",
                table: "QuickDropStep",
                column: "QuickDropId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_QuickDrops_QuickDropId",
                table: "OrderItems",
                column: "QuickDropId",
                principalTable: "QuickDrops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_QuickDrops_QuickDropId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "QuickDropStep");

            migrationBuilder.DropTable(
                name: "QuickDrops");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_QuickDropId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "QuickDropId",
                table: "OrderItems");
        }
    }
}
