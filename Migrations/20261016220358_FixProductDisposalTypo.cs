using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GroupProject.Migrations
{
    /// <inheritdoc />
    public partial class FixProductDisposalTypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProducDisposals");

            migrationBuilder.CreateTable(
                name: "ProductDisposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    DisposalLocationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DisposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDisposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDisposals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductDisposals_DisposalLocations_DisposalLocationId",
                        column: x => x.DisposalLocationId,
                        principalTable: "DisposalLocations",
                        principalColumn: "DisposalID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductDisposals_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductDisposals_DisposalLocationId",
                table: "ProductDisposals",
                column: "DisposalLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDisposals_ProductId",
                table: "ProductDisposals",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDisposals_UserId",
                table: "ProductDisposals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductDisposals");

            migrationBuilder.CreateTable(
                name: "ProducDisposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisposalLocationId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DisposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProducDisposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProducDisposals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProducDisposals_DisposalLocations_DisposalLocationId",
                        column: x => x.DisposalLocationId,
                        principalTable: "DisposalLocations",
                        principalColumn: "DisposalID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProducDisposals_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProducDisposals_DisposalLocationId",
                table: "ProducDisposals",
                column: "DisposalLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProducDisposals_ProductId",
                table: "ProducDisposals",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProducDisposals_UserId",
                table: "ProducDisposals",
                column: "UserId");
        }
    }
}
