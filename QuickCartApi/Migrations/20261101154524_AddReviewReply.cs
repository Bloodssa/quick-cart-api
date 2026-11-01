using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickCartApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "ProductReviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepliedAtUtc",
                table: "ProductReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReplyEditedAtUtc",
                table: "ProductReviews",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "RepliedAtUtc",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "ReplyEditedAtUtc",
                table: "ProductReviews");
        }
    }
}
