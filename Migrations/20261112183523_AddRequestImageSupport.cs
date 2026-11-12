using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestImageSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure ItemID is nullable on Images
            migrationBuilder.AlterColumn<int>(
                name: "ItemID",
                table: "Images",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                                              oldType: "integer");

            // Add RequestID column to Images
            migrationBuilder.AddColumn<int>(
                name: "RequestID",
                table: "Images",
                type: "integer",
                nullable: true);

            // Create index for RequestID
            migrationBuilder.CreateIndex(
                name: "IX_Images_RequestID",
                table: "Images",
                column: "RequestID");

            // Add Request foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Images_Requests_RequestID",
                table: "Images",
                column: "RequestID",
                principalTable: "Requests",
                principalColumn: "RequestID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Requests_RequestID",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_RequestID",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "RequestID",
                table: "Images");

            migrationBuilder.AlterColumn<int>(
                name: "ItemID",
                table: "Images",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                    oldClrType: typeof(int),
                                              oldType: "integer",
                                              oldNullable: true);
        }
    }
}
