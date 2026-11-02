using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickCartApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemRegularPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RegularPrice",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegularPrice",
                table: "OrderItems");
        }
    }
}
